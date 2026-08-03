
using Microsoft.EntityFrameworkCore;
using RetradeBE.Data;
using RetradeBE.Repositories;
using RetradeBE.Services;
using RetradeBE.Services.BackgroundJobs;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OData;
using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.SwaggerGen;
using RetradeBE.Mappings;
using RetradeBE.Hubs;
using RetradeBE.Models;

namespace RetradeBE
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
            builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
            LoadLocalDotEnvConfiguration(
                builder.Configuration,
                builder.Environment.ContentRootPath,
                builder.Environment.IsEnvironment("Docker"));
            builder.Configuration.AddEnvironmentVariables();

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            builder.Services.Configure<RetradeBE.Config.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.Configure<RetradeBE.Config.CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
            builder.Services.Configure<RetradeBE.Config.GoogleSettings>(builder.Configuration.GetSection("GoogleSettings"));
            builder.Services.Configure<RetradeBE.Config.VnPaySettings>(builder.Configuration.GetSection("VNPAY"));
            builder.Services.Configure<RetradeBE.Config.GhnSettings>(builder.Configuration.GetSection("GHN"));
            builder.Services.Configure<RetradeBE.Config.GeminiSettings>(builder.Configuration.GetSection("Gemini"));


            builder.Services.AddHttpClient();
            builder.Services.AddSignalR();
            builder.Services.AddHealthChecks();

            // Add services to the container.
            builder.Services.AddControllers()
                .AddOData(options => options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100))
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errors = string.Join(" ", context.ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage));
                        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(errors);
                    };
                });
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Tự động đăng ký tất cả các Repositories và Services bằng Reflection
            var assembly = typeof(Program).Assembly;
            
            var repositoryTypes = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("Repository") && !t.IsInterface && t.Name != "GenericRepository`1" && t.Name != "GenericRepository");
            foreach (var type in repositoryTypes)
            {
                var interfaceType = type.GetInterfaces().FirstOrDefault(i => i.Name == "I" + type.Name);
                if (interfaceType != null)
                {
                    builder.Services.AddScoped(interfaceType, type);
                }
            }

            var serviceTypes = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("Service") && !t.IsInterface);
            foreach (var type in serviceTypes)
            {
                var interfaceType = type.GetInterfaces().FirstOrDefault(i => i.Name == "I" + type.Name);
                if (interfaceType != null)
                {
                    builder.Services.AddScoped(interfaceType, type);
                }
            }

            builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfile>());
            builder.Services.AddControllers();
            builder.Services.AddHostedService<SubscriptionExpirationService>();
            builder.Services.AddHostedService<ShippingOutcomeSimulationService>();
            builder.Services.AddHostedService<AuctionClosingService>();
            builder.Services.AddMemoryCache(); // Thêm bộ nhớ đệm (dùng lưu OTP)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ReTrade API", Version = "v1" });
                c.OperationFilter<SwaggerODataFilter>();
                
                // Cấu hình Swagger để nhập Token
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
            });

            // Cấu hình JWT
            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<RetradeBE.Config.JwtSettings>();
            builder.Services.Configure<RetradeBE.Config.JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
            
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings!.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            // Lấy đường dẫn Frontend từ appsettings.json
            var frontendOrigins = GetFrontendOrigins(builder.Configuration, builder.Environment);

            // Thêm CORS để Frontend có thể gọi API (ví dụ: React, Vue, Angular chạy ở port khác)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy =>
                    {
                        policy.WithOrigins(frontendOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
            });

            builder.Services.AddHostedService<VoucherExpirationService>();

            var app = builder.Build();

            // Automatically apply migrations at startup
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await dbContext.Database.MigrateAsync();
                    Console.WriteLine("Database migrated successfully.");
                    SeedData(dbContext);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error applying migrations: {ex.Message}");
                    throw;
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment() ||
                app.Environment.IsEnvironment("Docker") ||
                app.Configuration.GetValue<bool>("Swagger:Enabled"))
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // app.UseHttpsRedirection();

            // Đăng ký Global Exception Middleware để bắt mọi lỗi phát sinh
            app.UseMiddleware<RetradeBE.Middlewares.GlobalExceptionMiddleware>();

            // Kích hoạt CORS (Phải đặt trước UseAuthorization)
            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();
            app.MapHealthChecks("/health");
            app.MapHub<RetradeBE.Hubs.AccountHub>("/hubs/accounts");
            app.MapHub<SellerHub>("/hubs/sellers");
            app.MapHub<OrderHub>("/hubs/orders");
            app.MapHub<AuctionHub>("/hubs/auctions");
            app.MapHub<ChatHub>("/hubs/chat");
            app.MapHub<NotificationHub>("/hubs/notifications");

            await app.RunAsync();
        }

        private static string[] GetFrontendOrigins(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var origins = new List<string>();

            AddOrigins(origins, configuration.GetValue<string>("FrontendUrl"));
            AddOrigins(origins, configuration.GetValue<string>("FrontendUrls"));
            AddOrigins(origins, configuration.GetValue<string>("Cors:AllowedOrigins"));
            var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            if (configuredOrigins is not null)
            {
                AddOrigins(origins, configuredOrigins);
            }

            if (environment.IsDevelopment() || environment.IsEnvironment("Docker"))
            {
                AddOrigins(origins,
                    "http://localhost:5173",
                    "http://127.0.0.1:5173",
                    "http://localhost:5174",
                    "http://127.0.0.1:5174");
            }

            return origins
                .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out _))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void LoadLocalDotEnvConfiguration(
            ConfigurationManager configuration,
            string contentRootPath,
            bool isDockerEnvironment)
        {
            var envPath = FindDotEnvPath(contentRootPath);
            if (envPath == null)
            {
                return;
            }

            var values = ParseDotEnvFile(envPath, isDockerEnvironment);
            if (values.Count > 0)
            {
                configuration.AddInMemoryCollection(values);
            }
        }

        private static string? FindDotEnvPath(string contentRootPath)
        {
            var directory = new DirectoryInfo(contentRootPath);

            for (var depth = 0; directory != null && depth < 8; depth++, directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, ".env");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Dictionary<string, string?> ParseDotEnvFile(string envPath, bool isDockerEnvironment)
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var rawLine in File.ReadAllLines(envPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                {
                    line = line["export ".Length..].TrimStart();
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = NormalizeDotEnvValue(line[(separatorIndex + 1)..].Trim());
                AddDotEnvValue(values, key, value, isDockerEnvironment);
            }

            return values;
        }

        private static void AddDotEnvValue(
            Dictionary<string, string?> values,
            string key,
            string value,
            bool isDockerEnvironment)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (!isDockerEnvironment &&
                IsDefaultConnectionStringKey(key) &&
                UsesDockerPostgresHost(value))
            {
                return;
            }

            values[key] = value;

            if (key.Contains("__", StringComparison.Ordinal))
            {
                values[key.Replace("__", ":")] = value;
            }

            switch (key.ToUpperInvariant())
            {
                case "GEMINI_API_KEY":
                    values["Gemini:ApiKey"] = value;
                    break;
                case "GEMINI_MODEL":
                    values["Gemini:Model"] = value;
                    break;
                case "JWT_SECRET":
                    values["JwtSettings:SecretKey"] = value;
                    break;
                case "FRONTEND_URL":
                    values["FrontendUrl"] = value;
                    break;
                case "VNPAY_CALLBACK_URL":
                    values["VNPAY:CallbackUrl"] = value;
                    break;
                case "VNPAY_IPN_URL":
                    values["VNPAY:IpnUrl"] = value;
                    break;
            }
        }

        private static bool IsDefaultConnectionStringKey(string key)
        {
            return string.Equals(key, "ConnectionStrings__DefaultConnection", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, "ConnectionStrings:DefaultConnection", StringComparison.OrdinalIgnoreCase);
        }

        private static bool UsesDockerPostgresHost(string connectionString)
        {
            return connectionString
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
                .Any(pair =>
                    pair.Length == 2 &&
                    (string.Equals(pair[0], "Host", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(pair[0], "Server", StringComparison.OrdinalIgnoreCase)) &&
                    string.Equals(pair[1], "db", StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeDotEnvValue(string value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];

                if (value.Contains('\\', StringComparison.Ordinal))
                {
                    value = value
                        .Replace("\\n", "\n")
                        .Replace("\\r", "\r")
                        .Replace("\\t", "\t")
                        .Replace("\\\"", "\"")
                        .Replace("\\\\", "\\");
                }

                return value;
            }

            var commentIndex = value.IndexOf(" #", StringComparison.Ordinal);
            return commentIndex >= 0 ? value[..commentIndex].TrimEnd() : value;
        }

        private static void AddOrigins(List<string> origins, params string?[] values)
        {
            foreach (var value in values.Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                origins.AddRange(value!
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(origin => origin.TrimEnd('/')));
            }
        }

        private static void SeedData(AppDbContext dbContext)
        {
            SeedRole(dbContext, 1, "Admin");
            SeedRole(dbContext, 2, "Buyer");
            SeedRole(dbContext, 3, "Seller");

            SeedUserAccount(dbContext, "usr_20260701_100001", "Admin", "System", "admin@retrade.com", "acc_20260701_100001", "admin", "Admin123@", 1);
            SeedUserAccount(dbContext, "usr_20260701_100002", "Demo", "Buyer", "buyer@retrade.com", "acc_20260701_100002", "buyer", "Buyer123@", 2);
            SeedUserAccount(dbContext, "usr_20260701_100003", "Demo", "Seller", "seller@retrade.com", "acc_20260701_100003", "seller", "Seller123@", 3);

            SeedAddress(dbContext, "adr_20260701_100001", "usr_20260701_100001", "Admin", "0900000001", "Tân Thạnh", 215, 2034, "570604");
            SeedAddress(dbContext, "adr_20260701_100003", "usr_20260701_100003", "Seller", "0900000002", "Đường số 1", 202, 3695, "90768");
            SeedAddress(dbContext, "adr_20260701_100002", "usr_20260701_100002", "Buyer", "0900000003", "Đường số 2", 201, 3440, "13010");

            SeedDemoOrders(dbContext);

            SeedServiceSubscription(
                dbContext,
                "sub_20260701_100001",
                "Seller Upgrade Package",
                "Buyer",
                99000m,
                30,
                "Unlock Seller privileges. Allowed to list products for sale. Professional store management.");

            SeedServiceSubscription(
                dbContext,
                "sub_20260701_100002",
                "Discount Voucher Package",
                "Buyer",
                49000m,
                30,
                "Receive 30 exclusive discount & freeship vouchers. Valid for 30 days of shopping. Unlock progressive savings every week.");

            SeedServiceSubscription(
                dbContext,
                "sub_20260701_100003",
                "Priority Listing Package",
                "Seller",
                69000m,
                30,
                "Activate priority display rights. Bring products to the top of search results. Reach tens of thousands of potential buyers.");

            SeedVoucher(dbContext, "voc_20260701_100001", "HELLORETRADE", "Fixed", 20000m, 100000m, null, 100);
            SeedVoucher(dbContext, "voc_20260701_100002", "SALE50", "Fixed", 50000m, 200000m, null, 100);
            SeedVoucher(dbContext, "voc_20260701_100003", "SAVE10", "Percentage", 10m, 150000m, 50000m, 100);
            SeedVoucher(dbContext, "voc_20260701_100004", "FREESHIP", "Fixed", 30000m, 50000m, null, 100);
            SeedVoucher(dbContext, "voc_20260701_100005", "WELCOME", "Percentage", 20m, 100000m, 100000m, 100);
            SeedVoucher(dbContext, "voc_20260701_100006", "EXPIRED20", "Fixed", 20000m, 50000m, null, 100, -20, -5);

            // Link vouchers to Demo Buyer (usr_20260701_100002)
            SeedMyVoucher(dbContext, "mvo_20260701_100001", "usr_20260701_100002", "voc_20260701_100001", "Active");
            SeedMyVoucher(dbContext, "mvo_20260701_100002", "usr_20260701_100002", "voc_20260701_100002", "Active");
            SeedMyVoucher(dbContext, "mvo_20260701_100003", "usr_20260701_100002", "voc_20260701_100003", "Used", DateTime.UtcNow.AddDays(-2));
            SeedMyVoucher(dbContext, "mvo_20260701_100004", "usr_20260701_100002", "voc_20260701_100006", "Active");

            // Update existing user vouchers in DB to reflect higher caps (min 50k max cap) and higher freeship values (30k-50k)
            var existingUserVouchers = dbContext.Set<Voucher>().Where(v => v.SellerId == null).ToList();
            foreach (var v in existingUserVouchers)
            {
                if (v.DiscountType == "Percentage" && v.MaxDiscountValue < 50000m)
                {
                    v.MaxDiscountValue = 50000m;
                    v.UpdatedAt = DateTime.UtcNow;
                }
                else if (v.DiscountType == "Fixed" && v.DiscountValue < 30000m)
                {
                    v.DiscountValue = 30000m;
                    v.MaxDiscountValue = 30000m;
                    v.UpdatedAt = DateTime.UtcNow;
                }
            }
            var ordersWithVouchers = dbContext.Order.Where(o => o.VoucherId != null).ToList();
            foreach (var ord in ordersWithVouchers)
            {
                var myVoucher = dbContext.MyVoucher.FirstOrDefault(mv => mv.UserId == ord.BuyerId && mv.VoucherId == ord.VoucherId);
                if (myVoucher != null && myVoucher.Status != "Used")
                {
                    myVoucher.Status = "Used";
                    myVoucher.UsedAt = ord.CreatedAt ?? DateTime.UtcNow;
                }
            }
            dbContext.SaveChanges();
            VoucherExpirationService.CheckAndExpireVouchersStaticAsync(dbContext).GetAwaiter().GetResult();

            // Seeding Refund Requests for Demo Buyer (usr_20260701_100002)
            SeedRefundRequest(dbContext, "ref_20260701_200001", "usr_20260701_100002", 150000m, "NotReady", "Auction refund for AUC_20260701_990001. Fee 10,000 VND retained.");
            SeedRefundRequest(dbContext, "ref_20260701_200002", "usr_20260701_100002", 200000m, "Pending", "Auction refund for AUC_20260701_990002. Fee 10,000 VND retained.", "Vietcombank", "0123456789", "BUYER TEST");
            SeedRefundRequest(dbContext, "ref_20260701_200003", "usr_20260701_100002", 50000m, "Processed", "Auction refund for AUC_20260701_990003. Fee 10,000 VND retained.", "Vietcombank", "0123456789", "BUYER TEST");
            SeedRefundRequest(dbContext, "ref_20260701_200004", "usr_20260701_100002", 300000m, "Completed", "Auction refund for AUC_20260701_990004.", "Vietcombank", "0123456789", "BUYER TEST");

            // Seeding Refund Requests for Demo Seller (usr_20260701_100003)
            SeedRefundRequest(dbContext, "ref_20260701_300001", "usr_20260701_100003", 500000m, "NotReady", "Auction refund for AUC_20260701_990005. Fee 10,000 VND retained.");
            SeedRefundRequest(dbContext, "ref_20260701_300002", "usr_20260701_100003", 750000m, "Pending", "Auction refund for AUC_20260701_990006. Fee 10,000 VND retained.", "Vietcombank", "0987654321", "SELLER TEST");

            // Seeding mock products and auctions for Demo Seller (usr_20260701_100003) to test all statuses
            SeedDemoProduct(
                dbContext,
                "prd_20260701_200001",
                "img_20260701_200001",
                "Vintage Leather Jacket",
                "High quality retro brown leather jacket in excellent condition.",
                null,
                "https://images.unsplash.com/photo-1551028719-00167b16eac5?w=600&auto=format&fit=crop&q=80",
                "cat_clothing",
                "Accepted",
                1);

            SeedDemoProduct(
                dbContext,
                "prd_20260701_200002",
                "img_20260701_200002",
                "Mechanical Keyboard",
                "RGB hot-swappable custom mechanical keyboard with linear switches.",
                null,
                "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=600&auto=format&fit=crop&q=80",
                "cat_computers",
                "Accepted",
                1);

            SeedDemoProduct(
                dbContext,
                "prd_20260701_200003",
                "img_20260701_200003",
                "Solid Wood Dining Table",
                "Rustic handmade solid wood dining table for family dinners.",
                null,
                "https://images.unsplash.com/photo-1577140917170-285929fb55b7?w=600&auto=format&fit=crop&q=80",
                "cat_furniture",
                "Accepted",
                1);

            // Attribute values for Vintage Leather Jacket (prd_20260701_200001)
            SeedProductAttributeValue(dbContext, "pav_lj_size", "prd_20260701_200001", "att_clot_size", "L");
            SeedProductAttributeValue(dbContext, "pav_lj_color", "prd_20260701_200001", "att_clot_color", "Brown");
            SeedProductAttributeValue(dbContext, "pav_lj_gender", "prd_20260701_200001", "att_clot_gender", "Men");
            SeedProductAttributeValue(dbContext, "pav_lj_material", "prd_20260701_200001", "att_clot_material", "Leather");
            SeedProductAttributeValue(dbContext, "pav_lj_brand", "prd_20260701_200001", "att_clot_brand", "Schott NYC");

            // Attribute values for Mechanical Keyboard (prd_20260701_200002)
            SeedProductAttributeValue(dbContext, "pav_kb_cpu", "prd_20260701_200002", "att_comp_cpu", "ARM Cortex M0");
            SeedProductAttributeValue(dbContext, "pav_kb_ram", "prd_20260701_200002", "att_comp_ram", "1");
            SeedProductAttributeValue(dbContext, "pav_kb_storage", "prd_20260701_200002", "att_comp_storage", "Flash Memory");
            SeedProductAttributeValue(dbContext, "pav_kb_gpu", "prd_20260701_200002", "att_comp_gpu", "None");
            SeedProductAttributeValue(dbContext, "pav_kb_os", "prd_20260701_200002", "att_comp_os", "Windows/macOS");

            // Attribute values for Solid Wood Dining Table (prd_20260701_200003)
            SeedProductAttributeValue(dbContext, "pav_dt_material", "prd_20260701_200003", "att_furn_material", "Oak Wood");
            SeedProductAttributeValue(dbContext, "pav_dt_color", "prd_20260701_200003", "att_furn_color", "Natural Oak");
            SeedProductAttributeValue(dbContext, "pav_dt_dimensions", "prd_20260701_200003", "att_furn_dimensions", "180x90x75 cm");
            SeedProductAttributeValue(dbContext, "pav_dt_brand", "prd_20260701_200003", "att_furn_brand", "Handcrafted");
            SeedProductAttributeValue(dbContext, "pav_dt_assembly", "prd_20260701_200003", "att_furn_assembly", "Yes");

            SeedAuction(
                dbContext,
                "auc_20260701_100001",
                "prd_20260701_200001",
                "usr_20260701_100003",
                350000m,
                350000m,
                20000m,
                600000m,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(2),
                "Upcoming");

            SeedAuction(
                dbContext,
                "auc_20260701_100002",
                "prd_20260701_200002",
                "usr_20260701_100003",
                150000m,
                210000m,
                10000m,
                400000m,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1),
                "Ongoing");

            SeedAuction(
                dbContext,
                "auc_20260701_100003",
                "prd_20260701_200003",
                "usr_20260701_100003",
                500000m,
                650000m,
                50000m,
                1000000m,
                DateTime.UtcNow.AddDays(-3),
                DateTime.UtcNow.AddDays(-2),
                "Ended");
        }

        private static void SeedRole(AppDbContext dbContext, int roleId, string name)
        {
            if (!dbContext.Role.Any(r => r.RoleId == roleId))
            {
                dbContext.Role.Add(new Role { RoleId = roleId, Name = name });
                dbContext.SaveChanges();
            }
        }

        private static void SeedUserAccount(
            AppDbContext dbContext,
            string userId, string firstName, string lastName, string email,
            string accountId, string username, string plainPassword, int roleId)
        {
            var user = dbContext.User.FirstOrDefault(u => u.UserId == userId);
            var account = dbContext.Account.FirstOrDefault(a => a.AccountId == accountId);
            var accountRoleExists = dbContext.AccountRole.Any(ar => ar.AccountId == accountId && ar.RoleId == roleId);

            if (user == null)
            {
                user = new User
                {
                    UserId = userId,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email
                };
                dbContext.User.Add(user);
            }
            else
            {
                user.Email = email;
            }

            if (account == null)
            {
                dbContext.Account.Add(new Account
                {
                    AccountId = accountId,
                    UserId = userId,
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                    Provider = "LOCAL",
                    Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString()
                });
            }
            else
            {
                account.Status = RetradeBE.Models.Enums.AccountStatusEnum.Active.ToString();
                account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
                account.Provider = "LOCAL";
                account.IsDeleted = false;
            }

            if (!accountRoleExists)
            {
                dbContext.AccountRole.Add(new AccountRole
                {
                    AccountId = accountId,
                    RoleId = roleId
                });
            }

            dbContext.SaveChanges();
        }

        private static void SeedAddress(
            AppDbContext dbContext,
            string addressId, string userId, string receiverName, string receiverPhone,
            string street, int provinceId, int districtId, string wardCode)
        {
            if (!dbContext.Address.Any(a => a.AddressId == addressId))
            {
                dbContext.Address.Add(new Address
                {
                    AddressId = addressId,
                    UserId = userId,
                    ReceiverName = receiverName,
                    ReceiverPhone = receiverPhone,
                    Street = street,
                    ProvinceId = provinceId,
                    DistrictId = districtId,
                    WardCode = wardCode,
                    IsDefault = true,
                    Status = "Active",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                dbContext.SaveChanges();
            }
        }

        private static void SeedDemoOrders(AppDbContext dbContext)
        {
            var now = DateTime.UtcNow;

            // 1. Seed Categories (Furniture, Computers, Mobile Phones, Clothing)
            SeedCategory(dbContext, "cat_furniture", "Furniture", "Table, chairs, beds, sofa, and other home furniture.");
            SeedCategory(dbContext, "cat_computers", "Computers", "Laptops, desktops, monitors, components, and computing accessories.");
            SeedCategory(dbContext, "cat_mobile_phones", "Mobile Phones", "Smartphones, basic phones, tablets, and mobile accessories.");
            SeedCategory(dbContext, "cat_clothing", "Clothing", "Menswear, womenswear, shoes, bags, and fashion accessories.");

            // 2. Seed Category Attributes (5 per Category, some required, some optional)
            // Mobile Phones (cat_mobile_phones)
            SeedCategoryAttribute(dbContext, "att_mobl_color", "cat_mobile_phones", "Color", "String", true, null, 1);
            SeedCategoryAttribute(dbContext, "att_mobl_storage", "cat_mobile_phones", "Storage Capacity", "String", true, "GB", 2);
            SeedCategoryAttribute(dbContext, "att_mobl_os", "cat_mobile_phones", "Operating System", "String", true, null, 3);
            SeedCategoryAttribute(dbContext, "att_mobl_screen", "cat_mobile_phones", "Screen Size", "String", false, "inches", 4);
            SeedCategoryAttribute(dbContext, "att_mobl_battery", "cat_mobile_phones", "Battery Capacity", "String", false, "mAh", 5);

            // Furniture (cat_furniture)
            SeedCategoryAttribute(dbContext, "att_furn_material", "cat_furniture", "Material", "String", true, null, 1);
            SeedCategoryAttribute(dbContext, "att_furn_color", "cat_furniture", "Color", "String", true, null, 2);
            SeedCategoryAttribute(dbContext, "att_furn_dimensions", "cat_furniture", "Dimensions", "String", false, "cm", 3);
            SeedCategoryAttribute(dbContext, "att_furn_brand", "cat_furniture", "Brand", "String", false, null, 4);
            SeedCategoryAttribute(dbContext, "att_furn_assembly", "cat_furniture", "Assembly Required", "String", false, null, 5);

            // Computers (cat_computers)
            SeedCategoryAttribute(dbContext, "att_comp_cpu", "cat_computers", "CPU", "String", true, null, 1);
            SeedCategoryAttribute(dbContext, "att_comp_ram", "cat_computers", "RAM", "String", true, "GB", 2);
            SeedCategoryAttribute(dbContext, "att_comp_storage", "cat_computers", "Storage", "String", true, null, 3);
            SeedCategoryAttribute(dbContext, "att_comp_gpu", "cat_computers", "GPU", "String", false, null, 4);
            SeedCategoryAttribute(dbContext, "att_comp_os", "cat_computers", "Operating System", "String", false, null, 5);

            // Clothing (cat_clothing)
            SeedCategoryAttribute(dbContext, "att_clot_size", "cat_clothing", "Size", "String", true, null, 1);
            SeedCategoryAttribute(dbContext, "att_clot_color", "cat_clothing", "Color", "String", true, null, 2);
            SeedCategoryAttribute(dbContext, "att_clot_gender", "cat_clothing", "Gender", "String", false, null, 3);
            SeedCategoryAttribute(dbContext, "att_clot_material", "cat_clothing", "Material", "String", false, null, 4);
            SeedCategoryAttribute(dbContext, "att_clot_brand", "cat_clothing", "Brand", "String", false, null, 5);

            // 3. Seed Products with Category mapping
            SeedDemoProduct(
                dbContext,
                "prd_20260701_100001",
                "img_20260701_100001",
                "Iphone 13 pro max 256gb - 99%",
                "Second-hand phone used for testing order list.",
                1250000m,
                "https://cdn2.cellphones.com.vn/x/media/catalog/product/i/p/iphone-13-pro-max-256gb-cu-dep_2_.png",
                "cat_mobile_phones",
                "Accepted",
                5,
                "LikeNew");

            SeedDemoProduct(
                dbContext,
                "prd_20260701_100002",
                "img_20260701_100002",
                "Tai nghe sony wh-1000xm4 - likenew",
                "Tai nghe không dây chính hãng giá tốt, còn bảo hành 6 tháng.",
                650000m,
                "https://tainghetot.com/wp-content/uploads/2021/06/P1220334.jpg",
                "cat_mobile_phones",
                "Accepted",
                5,
                "Fair");

            SeedDemoProduct(
                dbContext,
                "prd_20260701_100003",
                "img_20260701_100003",
                "iPhone 11 Pro Max",
                "iPhone 11 Pro Max 64GB like new 99% là máy đa qua sử dụng\r\nnhưng ngoại hình còn đẹp như mới.\r\nCam kết nguyên bản 100%, bảo hành quốc tế trọn đời, dùng được\r\nESIM, 1 đền 10 nếu phát hiện hàng giả, 1 đổi 1 trong 30 ngày.\r\nGiá tốt nhất thị trường, hỗ trợ góp 0%, Ship COD toàn quốc.\r\nViettablet trợ giá thu cũ lên đời lên đến 500.000đ cho tất cả sản\r\nphẩm smartphone, tablet.",
                null,
                "https://cdn.viettablet.com/images/companies/1/0-hinh-moi/thai/iphone%2011%20pro%20max/iphone-11-pro-max-cu-likenew.png?1672304168621",
                "cat_mobile_phones",
                "Ready",
                1);

            // 4. Seed Product Attribute Values
            // iPhone 13 Pro Max
            SeedProductAttributeValue(dbContext, "pav_ip_color", "prd_20260701_100001", "att_mobl_color", "Sierra Blue");
            SeedProductAttributeValue(dbContext, "pav_ip_storage", "prd_20260701_100001", "att_mobl_storage", "256");
            SeedProductAttributeValue(dbContext, "pav_ip_os", "prd_20260701_100001", "att_mobl_os", "iOS");
            SeedProductAttributeValue(dbContext, "pav_ip_screen", "prd_20260701_100001", "att_mobl_screen", "6.7");
            SeedProductAttributeValue(dbContext, "pav_ip_battery", "prd_20260701_100001", "att_mobl_battery", "4352");

            // Sony Headphones
            SeedProductAttributeValue(dbContext, "pav_so_color", "prd_20260701_100002", "att_mobl_color", "Black");
            SeedProductAttributeValue(dbContext, "pav_so_storage", "prd_20260701_100002", "att_mobl_storage", "N/A");
            SeedProductAttributeValue(dbContext, "pav_so_os", "prd_20260701_100002", "att_mobl_os", "Proprietary");

            // Test Auction Product
            SeedProductAttributeValue(dbContext, "pav_ta_color", "prd_20260701_100003", "att_mobl_color", "Black");
            SeedProductAttributeValue(dbContext, "pav_ta_storage", "prd_20260701_100003", "att_mobl_storage", "128");
            SeedProductAttributeValue(dbContext, "pav_ta_os", "prd_20260701_100003", "att_mobl_os", "Android");

            // 5. Seed Additional Products per Category (Real Names)
            // --- Furniture Category ---
            SeedDemoProduct(
                dbContext,
                "prd_furn_001",
                "img_furn_001",
                "Nordic Fabric Sofa 3-Seater",
                "Nordic style comfortable 3-seater sofa with washable fabric cover.",
                4500000m,
                "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?w=600&auto=format&fit=crop&q=80",
                "cat_furniture");
            SeedProductAttributeValue(dbContext, "pav_f1_mat", "prd_furn_001", "att_furn_material", "Fabric");
            SeedProductAttributeValue(dbContext, "pav_f1_col", "prd_furn_001", "att_furn_color", "Grey");
            SeedProductAttributeValue(dbContext, "pav_f1_dim", "prd_furn_001", "att_furn_dimensions", "210x85x80 cm");
            SeedProductAttributeValue(dbContext, "pav_f1_brd", "prd_furn_001", "att_furn_brand", "IKEA");
            SeedProductAttributeValue(dbContext, "pav_f1_asm", "prd_furn_001", "att_furn_assembly", "No");

            SeedDemoProduct(
                dbContext,
                "prd_furn_002",
                "img_furn_002",
                "Minimalist Wooden Coffee Table",
                "Elegant solid oak wood coffee table for living rooms.",
                1800000m,
                "https://images.unsplash.com/photo-1533090161767-e6ffed986c88?w=600&auto=format&fit=crop&q=80",
                "cat_furniture",
                "Accepted",
                5,
                "LikeNew");
            SeedProductAttributeValue(dbContext, "pav_f2_mat", "prd_furn_002", "att_furn_material", "Oak Wood");
            SeedProductAttributeValue(dbContext, "pav_f2_col", "prd_furn_002", "att_furn_color", "Light Brown");
            SeedProductAttributeValue(dbContext, "pav_f2_dim", "prd_furn_002", "att_furn_dimensions", "120x60x45 cm");
            SeedProductAttributeValue(dbContext, "pav_f2_brd", "prd_furn_002", "att_furn_brand", "Jysk");
            SeedProductAttributeValue(dbContext, "pav_f2_asm", "prd_furn_002", "att_furn_assembly", "Yes");

            SeedDemoProduct(
                dbContext,
                "prd_furn_003",
                "img_furn_003",
                "Ergonomic Office Chair",
                "Premium ergonomic mesh office chair with adjustable lumbar support.",
                8500000m,
                "https://images.unsplash.com/photo-1505797149-43b0069ec26b?w=600&auto=format&fit=crop&q=80",
                "cat_furniture",
                "Sold",
                0,
                "Used");
            SeedProductAttributeValue(dbContext, "pav_f3_mat", "prd_furn_003", "att_furn_material", "Mesh/Steel");
            SeedProductAttributeValue(dbContext, "pav_f3_col", "prd_furn_003", "att_furn_color", "Black");
            SeedProductAttributeValue(dbContext, "pav_f3_dim", "prd_furn_003", "att_furn_dimensions", "65x65x120 cm");
            SeedProductAttributeValue(dbContext, "pav_f3_brd", "prd_furn_003", "att_furn_brand", "Herman Miller");
            SeedProductAttributeValue(dbContext, "pav_f3_asm", "prd_furn_003", "att_furn_assembly", "Yes");

            // --- Computers Category ---
            SeedDemoProduct(
                dbContext,
                "prd_comp_001",
                "img_comp_001",
                "MacBook Pro 14 M3 Pro",
                "Apple MacBook Pro 14-inch with M3 Pro chip, 18GB Unified Memory, 512GB SSD.",
                48000000m,
                "https://onewaymobile.vn/images/news/2024/10/27/original/macbook-pro-14-m3-vs-m3-pro_1730042674.jpeg",
                "cat_computers",
                "Accepted",
                5,
                "New");
            SeedProductAttributeValue(dbContext, "pav_c1_cpu", "prd_comp_001", "att_comp_cpu", "Apple M3 Pro");
            SeedProductAttributeValue(dbContext, "pav_c1_ram", "prd_comp_001", "att_comp_ram", "18");
            SeedProductAttributeValue(dbContext, "pav_c1_sto", "prd_comp_001", "att_comp_storage", "512GB SSD");
            SeedProductAttributeValue(dbContext, "pav_c1_gpu", "prd_comp_001", "att_comp_gpu", "14-core GPU");
            SeedProductAttributeValue(dbContext, "pav_c1_os", "prd_comp_001", "att_comp_os", "macOS");

            SeedDemoProduct(
                dbContext,
                "prd_comp_002",
                "img_comp_002",
                "ASUS ROG Strix G16 Gaming Laptop",
                "Powerful gaming laptop with Intel i7-13650HX, RTX 4060, 16GB RAM, 512GB SSD.",
                29500000m,
                "https://cdn.tgdd.vn/Products/Images/44/305664/asus-gaming-rog-strix-g16-g614ju-i7-n3777w-1-750x500.jpg",
                "cat_computers",
                "Accepted",
                5,
                "LikeNew");
            SeedProductAttributeValue(dbContext, "pav_c2_cpu", "prd_comp_002", "att_comp_cpu", "Intel Core i7-13650HX");
            SeedProductAttributeValue(dbContext, "pav_c2_ram", "prd_comp_002", "att_comp_ram", "16");
            SeedProductAttributeValue(dbContext, "pav_c2_sto", "prd_comp_002", "att_comp_storage", "512GB SSD");
            SeedProductAttributeValue(dbContext, "pav_c2_gpu", "prd_comp_002", "att_comp_gpu", "NVIDIA RTX 4060");
            SeedProductAttributeValue(dbContext, "pav_c2_os", "prd_comp_002", "att_comp_os", "Windows 11");

            SeedDemoProduct(
                dbContext,
                "prd_comp_003",
                "img_comp_003",
                "Dell UltraSharp 27 Monitor",
                "27-inch 4K USB-C Hub Monitor with high color accuracy.",
                11500000m,
                "https://npcshop.vn/media/product/1790-u2719d_lfp_00000f000_gy_v1.jpg",
                "cat_computers",
                "Sold",
                0,
                "Fair");
            SeedProductAttributeValue(dbContext, "pav_c3_cpu", "prd_comp_003", "att_comp_cpu", "N/A");
            SeedProductAttributeValue(dbContext, "pav_c3_ram", "prd_comp_003", "att_comp_ram", "N/A");
            SeedProductAttributeValue(dbContext, "pav_c3_sto", "prd_comp_003", "att_comp_storage", "N/A");
            SeedProductAttributeValue(dbContext, "pav_c3_gpu", "prd_comp_003", "att_comp_gpu", "N/A");
            SeedProductAttributeValue(dbContext, "pav_c3_os", "prd_comp_003", "att_comp_os", "Universal");

            // --- Mobile Phones Category ---
            SeedDemoProduct(
                dbContext,
                "prd_mobl_001",
                "img_mobl_001",
                "Samsung Galaxy S24 Ultra",
                "Flagship phone with Snapdragon 8 Gen 3, 12GB RAM, 512GB storage, and S-Pen.",
                null,
                "https://product.hstatic.net/1000370129/product/8e64caf8243aed0fad6c81b_master_7830250f33864a0eb199702dee8fd9ea_master_af3aca615315449dbe741862b5dd9556_master.jpg",
                "cat_mobile_phones",
                "Ready",
                1);
            SeedProductAttributeValue(dbContext, "pav_m1_col", "prd_mobl_001", "att_mobl_color", "Titanium Gray");
            SeedProductAttributeValue(dbContext, "pav_m1_sto", "prd_mobl_001", "att_mobl_storage", "512");
            SeedProductAttributeValue(dbContext, "pav_m1_os", "prd_mobl_001", "att_mobl_os", "Android 14");
            SeedProductAttributeValue(dbContext, "pav_m1_scr", "prd_mobl_001", "att_mobl_screen", "6.8");
            SeedProductAttributeValue(dbContext, "pav_m1_bat", "prd_mobl_001", "att_mobl_battery", "5000");

            SeedDemoProduct(
                dbContext,
                "prd_mobl_002",
                "img_mobl_002",
                "iPhone 15 Pro",
                "Apple flagship smartphone with Titanium design and A17 Pro chip.",
                24500000m,
                "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-plus-256gb_2.png",
                "cat_mobile_phones",
                "Accepted",
                5,
                "New");
            SeedProductAttributeValue(dbContext, "pav_m2_col", "prd_mobl_002", "att_mobl_color", "Natural Titanium");
            SeedProductAttributeValue(dbContext, "pav_m2_sto", "prd_mobl_002", "att_mobl_storage", "128");
            SeedProductAttributeValue(dbContext, "pav_m2_os", "prd_mobl_002", "att_mobl_os", "iOS 17");
            SeedProductAttributeValue(dbContext, "pav_m2_scr", "prd_mobl_002", "att_mobl_screen", "6.1");
            SeedProductAttributeValue(dbContext, "pav_m2_bat", "prd_mobl_002", "att_mobl_battery", "3274");

            SeedDemoProduct(
                dbContext,
                "prd_mobl_003",
                "img_mobl_003",
                "iPad Air 5 M1",
                "Thin and light tablet with Apple M1 chip, 10.9-inch Liquid Retina display.",
                13900000m,
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ0tNuMBK9kQSDqLgfMI2dHznfpknbZSM-M4ngstPaNwsQvCbwJLXR3BIT4&s=10",
                "cat_mobile_phones",
                "Sold",
                0,
                "LikeNew");
            SeedProductAttributeValue(dbContext, "pav_m3_col", "prd_mobl_003", "att_mobl_color", "Space Gray");
            SeedProductAttributeValue(dbContext, "pav_m3_sto", "prd_mobl_003", "att_mobl_storage", "64");
            SeedProductAttributeValue(dbContext, "pav_m3_os", "prd_mobl_003", "att_mobl_os", "iPadOS");
            SeedProductAttributeValue(dbContext, "pav_m3_scr", "prd_mobl_003", "att_mobl_screen", "10.9");
            SeedProductAttributeValue(dbContext, "pav_m3_bat", "prd_mobl_003", "att_mobl_battery", "7600");

            // --- Clothing Category ---
            SeedDemoProduct(
                dbContext,
                "prd_clot_001",
                "img_clot_001",
                "Uniqlo Airism Cotton Oversized T-Shirt",
                "Oversized t-shirt with premium Airism fabric blend, perfect for summer.",
                290000m,
                "https://image.uniqlo.com/UQ/ST3/WesternCommon/imagesgoods/425974/sub/goods_425974_sub14_3x4.jpg?width=600",
                "cat_clothing");
            SeedProductAttributeValue(dbContext, "pav_cl1_siz", "prd_clot_001", "att_clot_size", "M");
            SeedProductAttributeValue(dbContext, "pav_cl1_col", "prd_clot_001", "att_clot_color", "White");
            SeedProductAttributeValue(dbContext, "pav_cl1_gen", "prd_clot_001", "att_clot_gender", "Unisex");
            SeedProductAttributeValue(dbContext, "pav_cl1_mat", "prd_clot_001", "att_clot_material", "Cotton/Polyester");
            SeedProductAttributeValue(dbContext, "pav_cl1_brd", "prd_clot_001", "att_clot_brand", "Uniqlo");

            SeedDemoProduct(
                dbContext,
                "prd_clot_002",
                "img_clot_002",
                "Nike Air Force 1 '07 Sneakers",
                "All-time classic white leather sneakers from Nike.",
                2200000m,
                "https://static.nike.com/a/images/t_web_pdp_936_v2/f_auto,u_9ddf04c7-2a9a-4d76-add1-d15af8f0263d,c_scale,fl_relative,w_1.0,h_1.0,fl_layer_apply/b7d9211c-26e7-431a-ac24-b0540fb3c00f/AIR+FORCE+1+%2707.png",
                "cat_clothing",
                "Accepted",
                5,
                "New");
            SeedProductAttributeValue(dbContext, "pav_cl2_siz", "prd_clot_002", "att_clot_size", "42");
            SeedProductAttributeValue(dbContext, "pav_cl2_col", "prd_clot_002", "att_clot_color", "White");
            SeedProductAttributeValue(dbContext, "pav_cl2_gen", "prd_clot_002", "att_clot_gender", "Men");
            SeedProductAttributeValue(dbContext, "pav_cl2_mat", "prd_clot_002", "att_clot_material", "Leather");
            SeedProductAttributeValue(dbContext, "pav_cl2_brd", "prd_clot_002", "att_clot_brand", "Nike");

            SeedDemoProduct(
                dbContext,
                "prd_clot_003",
                "img_clot_003",
                "Levis 501 Original Fit Jeans",
                "Straight fit button-fly original denim jeans in medium stone wash.",
                1500000m,
                "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSho97kVA7D92umJWK3CYprERV_rJ74pjNjfhaPoAUk1Q&s",
                "cat_clothing",
                "Pending",
                5,
                "Used");
            SeedProductAttributeValue(dbContext, "pav_cl3_siz", "prd_clot_003", "att_clot_size", "32/32");
            SeedProductAttributeValue(dbContext, "pav_cl3_col", "prd_clot_003", "att_clot_color", "Blue Denim");
            SeedProductAttributeValue(dbContext, "pav_cl3_gen", "prd_clot_003", "att_clot_gender", "Men");
            SeedProductAttributeValue(dbContext, "pav_cl3_mat", "prd_clot_003", "att_clot_material", "Cotton Denim");
            SeedProductAttributeValue(dbContext, "pav_cl3_brd", "prd_clot_003", "att_clot_brand", "Levi's");

            SeedDemoOrder(
                dbContext,
                "ord_20260701_100001",
                "RTD-2026-0001",
                "prd_20260701_100001",
                1,
                1250000m,
                30000m,
                0m,
                "Pending",
                "pay_20260701_100001",
                "Pending",
                now.AddDays(-2));

            SeedDemoOrder(
                dbContext,
                "ord_20260701_100002",
                "RTD-2026-0002",
                "prd_20260701_100002",
                2,
                650000m,
                25000m,
                50000m,
                "Shipping",
                "pay_20260701_100002",
                "Paid",
                now.AddDays(-1));

            dbContext.SaveChanges();
        }

        private static void SeedDemoProduct(
            AppDbContext dbContext,
            string productId,
            string imageId,
            string name,
            string description,
            decimal? price,
            string imageUrl,
            string categoryId,
            string status = "Accepted",
            int stockQuantity = 5,
            string condition = "Used")
        {
            var now = DateTime.UtcNow;

            if (!dbContext.Image.Any(i => i.ImageId == imageId))
            {
                dbContext.Image.Add(new Image
                {
                    ImageId = imageId,
                    ImageUrl = imageUrl,
                    AltText = name,
                    CreatedAt = now
                });
            }

            if (!dbContext.Product.Any(p => p.ProductId == productId))
            {
                dbContext.Product.Add(new Product
                {
                    ProductId = productId,
                    SellerId = "usr_20260701_100003",
                    CategoryId = categoryId,
                    Name = name,
                    Description = description,
                    Condition = condition,
                    Price = price,
                    StockQuantity = stockQuantity,
                    Status = status,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            if (!dbContext.ProductImage.Any(pi => pi.ProductId == productId && pi.ImageId == imageId))
            {
                dbContext.ProductImage.Add(new ProductImage
                {
                    ProductId = productId,
                    ImageId = imageId,
                    IsMain = true,
                    SortOrder = 1,
                    CreatedAt = now
                });
            }
        }

        private static void SeedAuction(
            AppDbContext dbContext,
            string auctionId,
            string productId,
            string sellerId,
            decimal startingPrice,
            decimal currentPrice,
            decimal minIncrement,
            decimal? buyNowPrice,
            DateTime startTime,
            DateTime endTime,
            string status)
        {
            if (!dbContext.Auction.Any(a => a.AuctionId == auctionId))
            {
                dbContext.Auction.Add(new Auction
                {
                    AuctionId = auctionId,
                    ProductId = productId,
                    SellerId = sellerId,
                    StartingPrice = startingPrice,
                    CurrentPrice = currentPrice,
                    MinIncrement = minIncrement,
                    BuyNowPrice = buyNowPrice,
                    StartTime = startTime,
                    EndTime = endTime,
                    Status = status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                dbContext.SaveChanges();
            }
        }

        private static void SeedCategory(AppDbContext dbContext, string categoryId, string name, string description)
        {
            var category = dbContext.Category.FirstOrDefault(c => c.CategoryId == categoryId);
            if (category == null)
            {
                dbContext.Category.Add(new Category
                {
                    CategoryId = categoryId,
                    Name = name,
                    Description = description,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                dbContext.SaveChanges();
            }
        }

        private static void SeedCategoryAttribute(
            AppDbContext dbContext,
            string attributeId,
            string categoryId,
            string name,
            string dataType,
            bool isRequired,
            string? unit = null,
            int displayOrder = 1)
        {
            var attr = dbContext.Attributes.FirstOrDefault(a => a.AttributeId == attributeId);
            if (attr == null)
            {
                dbContext.Attributes.Add(new Attributes
                {
                    AttributeId = attributeId,
                    CategoryId = categoryId,
                    Name = name,
                    DataType = dataType,
                    IsRequired = isRequired,
                    Unit = unit,
                    DisplayOrder = displayOrder,
                    IsFilterable = true,
                    IsSearchable = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                dbContext.SaveChanges();
            }
        }

        private static void SeedProductAttributeValue(
            AppDbContext dbContext,
            string productAttributeId,
            string productId,
            string attributeId,
            string value)
        {
            var val = dbContext.ProductAttribute
                .FirstOrDefault(pa => pa.ProductId == productId && pa.AttributeId == attributeId);
            if (val == null)
            {
                dbContext.ProductAttribute.Add(new ProductAttribute
                {
                    ProductAttributeId = productAttributeId,
                    ProductId = productId,
                    AttributeId = attributeId,
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                dbContext.SaveChanges();
            }
        }

        private static void SeedDemoOrder(
            AppDbContext dbContext,
            string orderId,
            string orderCode,
            string productId,
            int quantity,
            decimal unitPrice,
            decimal shippingFee,
            decimal discountAmount,
            string status,
            string paymentId,
            string paymentStatus,
            DateTime createdAt)
        {
            var finalAmount = unitPrice * quantity + shippingFee - discountAmount;

            if (!dbContext.Order.Any(o => o.OrderId == orderId))
            {
                dbContext.Order.Add(new Order
                {
                    OrderId = orderId,
                    OrderCode = orderCode,
                    BuyerId = "usr_20260701_100002",
                    SellerId = "usr_20260701_100003",
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    AddressSnapshot = "Demo Buyer, 123 Test Street, District 1, Ho Chi Minh City",
                    TrackingCode = status == "Shipping" ? "DEMO-TRACK-002" : null,
                    ShippingProvider = status == "Shipping" ? "Demo Express" : null,
                    TotalAmount = unitPrice * quantity,
                    ShippingFee = shippingFee,
                    DiscountAmount = discountAmount,
                    FinalAmount = finalAmount,
                    ExpectedDeliveryTime = createdAt.AddDays(5),
                    Status = status,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });
            }

            if (!dbContext.Payment.Any(p => p.PaymentId == paymentId))
            {
                dbContext.Payment.Add(new Payment
                {
                    PaymentId = paymentId,
                    OrderId = orderId,
                    UserId = "usr_20260701_100002",
                    Amount = finalAmount,
                    PaymentMethod = "VNPAY",
                    ProviderTransactionId = paymentStatus == "Paid" ? "VNPAY-DEMO-002" : null,
                    Status = paymentStatus,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });
            }
        }

        private static void SeedServiceSubscription(
            AppDbContext dbContext,
            string serviceId,
            string name,
            string targetRole,
            decimal price,
            int durationDays,
            string benefitsDescription)
        {
            var existingService = dbContext.ServiceSubscription.FirstOrDefault(s => s.ServiceId == serviceId);
            if (existingService != null)
            {
                existingService.Name = name;
                existingService.TargetRole = targetRole;
                existingService.Price = price;
                existingService.DurationDays = durationDays;
                existingService.BenefitsDescription = benefitsDescription;
            }
            else
            {
                dbContext.ServiceSubscription.Add(new ServiceSubscription
                {
                    ServiceId = serviceId,
                    Name = name,
                    TargetRole = targetRole,
                    Price = price,
                    DurationDays = durationDays,
                    BenefitsDescription = benefitsDescription,
                    CreatedAt = DateTime.UtcNow
                });
            }

            dbContext.SaveChanges();
        }

        private static void SeedVoucher(
            AppDbContext dbContext,
            string voucherId,
            string code,
            string discountType,
            decimal discountValue,
            decimal minOrderValue,
            decimal? maxDiscountValue,
            int quantity,
            int startDaysOffset = -10,
            int expiryDaysOffset = 30)
        {
            var now = DateTime.UtcNow;
            var existing = dbContext.Voucher.FirstOrDefault(v => v.VoucherId == voucherId);
            if (existing != null)
            {
                existing.Code = code;
                existing.DiscountType = discountType;
                existing.DiscountValue = discountValue;
                existing.MinOrderValue = minOrderValue;
                existing.MaxDiscountValue = maxDiscountValue;
                existing.Quantity = quantity;
                existing.StartDate = now.AddDays(startDaysOffset);
                existing.ExpirationDate = now.AddDays(expiryDaysOffset);
            }
            else
            {
                dbContext.Voucher.Add(new Voucher
                {
                    VoucherId = voucherId,
                    SellerId = null,
                    Code = code,
                    DiscountType = discountType,
                    DiscountValue = discountValue,
                    MinOrderValue = minOrderValue,
                    MaxDiscountValue = maxDiscountValue,
                    Quantity = quantity,
                    StartDate = now.AddDays(startDaysOffset),
                    ExpirationDate = now.AddDays(expiryDaysOffset),
                    Status = "Active",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            dbContext.SaveChanges();
        }

        private static void SeedMyVoucher(
            AppDbContext dbContext,
            string userVoucherId,
            string userId,
            string voucherId,
            string status,
            DateTime? usedAt = null)
        {
            var existing = dbContext.MyVoucher.FirstOrDefault(mv => mv.UserVoucherId == userVoucherId);
            if (existing != null)
            {
                existing.Status = status;
                existing.UsedAt = usedAt;
            }
            else
            {
                dbContext.MyVoucher.Add(new MyVoucher
                {
                    UserVoucherId = userVoucherId,
                    UserId = userId,
                    VoucherId = voucherId,
                    Status = status,
                    UsedAt = usedAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            dbContext.SaveChanges();
        }

        private static void SeedRefundRequest(
            AppDbContext dbContext,
            string refundRequestId,
            string userId,
            decimal amount,
            string status,
            string note,
            string? bankName = null,
            string? bankAccountNumber = null,
            string? bankAccountHolder = null)
        {
            var existing = dbContext.RefundRequest.FirstOrDefault(r => r.RefundRequestId == refundRequestId);
            if (existing != null)
            {
                existing.Status = status;
                existing.Amount = amount;
                existing.Note = note;
                existing.BankName = bankName;
                existing.BankAccountNumber = bankAccountNumber;
                existing.BankAccountHolder = bankAccountHolder;
            }
            else
            {
                dbContext.RefundRequest.Add(new RefundRequest
                {
                    RefundRequestId = refundRequestId,
                    UserId = userId,
                    Amount = amount,
                    Status = status,
                    Note = note,
                    BankName = bankName,
                    BankAccountNumber = bankAccountNumber,
                    BankAccountHolder = bankAccountHolder,
                    RequestedAt = DateTime.UtcNow.AddDays(-2),
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                });
            }
            dbContext.SaveChanges();
        }
    }


    public class SwaggerODataFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.RequestBody?.Content != null)
            {
                var keysToRemove = operation.RequestBody.Content.Keys.Where(k => k.Contains("odata")).ToList();
                foreach (var key in keysToRemove) operation.RequestBody.Content.Remove(key);
            }
            foreach (var response in operation.Responses.Values)
            {
                if (response.Content != null)
                {
                    var keysToRemove = response.Content.Keys.Where(k => k.Contains("odata")).ToList();
                    foreach (var key in keysToRemove) response.Content.Remove(key);
                }
            }
        }
    }
}
