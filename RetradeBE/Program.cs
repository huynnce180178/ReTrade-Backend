
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

            builder.Services.AddHealthChecks()
                .AddDbContextCheck<AppDbContext>("Database");

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
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    var dbContext = services.GetRequiredService<AppDbContext>();

                    // Log Target Database Information safely (without password)
                    var rawConnStr = app.Configuration.GetConnectionString("DefaultConnection");
                    if (!string.IsNullOrEmpty(rawConnStr))
                    {
                        var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder(rawConnStr);
                        logger.LogInformation("Target Database Info: Host={Host}, Port={Port}, Database={Database}, User={User}",
                            npgsqlBuilder.Host, npgsqlBuilder.Port, npgsqlBuilder.Database, npgsqlBuilder.Username);
                    }

                    var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
                    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();

                    logger.LogInformation("Database Migration Status: Applied={AppliedCount}, Pending={PendingCount}",
                        appliedMigrations.Count, pendingMigrations.Count);

                    if (pendingMigrations.Any())
                    {
                        logger.LogInformation("Applying {Count} pending migration(s): [{Migrations}]",
                            pendingMigrations.Count, string.Join(", ", pendingMigrations));

                        await dbContext.Database.MigrateAsync();

                        logger.LogInformation("Database migration completed successfully.");
                    }
                    else
                    {
                        logger.LogInformation("Database schema is up to date. No pending migrations to apply.");
                    }

                    logger.LogInformation("Seeding essential system master data (Roles, Admin account, Service Subscriptions)...");
                    SeedEssentialMasterData(dbContext);

                    if (app.Environment.IsDevelopment())
                    {
                        logger.LogInformation("Development environment detected. Seeding demo products and orders...");
                        SeedDemoData(dbContext);
                    }
                    else
                    {
                        logger.LogInformation("Non-Development environment ({Env}) detected. Skipping demo data seeding.", app.Environment.EnvironmentName);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "FATAL ERROR: Database migration failed. Aborting application startup.");
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
            app.MapHub<OfferHub>("/hubs/offers");
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

        private static void SeedEssentialMasterData(AppDbContext dbContext)
        {
            // 1. System Roles (Required for system permissions)
            SeedRole(dbContext, 1, "Admin");
            SeedRole(dbContext, 2, "Buyer");
            SeedRole(dbContext, 3, "Seller");

            // 2. System Admin Account (Required for Admin portal access)
            SeedUserAccount(dbContext, "usr_20260701_100001", "Admin", "System", "admin@retrade.com", "acc_20260701_100001", "admin", "Admin123@", 1, "0769331645");
            SeedAddress(dbContext, "adr_20260701_100001", "usr_20260701_100001", "Admin", "0769331645", "Hẻm 226/16, Phường An Bình, Quận Ninh Kiều, Cần Thơ", 215, 2034, "570604");

            // 3. Essential System Service Subscriptions
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
        }

        private static void SeedDemoData(AppDbContext dbContext)
        {
            // Demo data seeding is disabled to keep database clean.
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
            string accountId, string username, string plainPassword, int roleId,
            string? phone = null)
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
                    Email = email,
                    Phone = phone
                };
                dbContext.User.Add(user);
            }
            else
            {
                user.Email = email;
                if (!string.IsNullOrEmpty(phone))
                {
                    user.Phone = phone;
                }
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
