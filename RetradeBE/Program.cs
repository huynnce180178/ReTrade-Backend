
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
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<RetradeBE.Config.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.Configure<RetradeBE.Config.CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
            builder.Services.Configure<RetradeBE.Config.GoogleSettings>(builder.Configuration.GetSection("GoogleSettings"));
            builder.Services.Configure<RetradeBE.Config.VnPaySettings>(builder.Configuration.GetSection("VNPAY"));
            builder.Services.Configure<RetradeBE.Config.GhnSettings>(builder.Configuration.GetSection("GHN"));

            builder.Services.AddHttpClient();
            builder.Services.AddSignalR();

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
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
            var frontendUrl = builder.Configuration.GetValue<string>("FrontendUrl") ?? "http://localhost:5173";
            var frontendOrigins = new[]
            {
                frontendUrl,
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:5174",
                "http://127.0.0.1:5174"
            }.Distinct().ToArray();

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

            var app = builder.Build();

            // Automatically apply migrations at startup
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    dbContext.Database.Migrate();
                    Console.WriteLine("Database migrated successfully.");
                    SeedData(dbContext);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error applying migrations: {ex.Message}");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Đăng ký Global Exception Middleware để bắt mọi lỗi phát sinh
            app.UseMiddleware<RetradeBE.Middlewares.GlobalExceptionMiddleware>();

            // Kích hoạt CORS (Phải đặt trước UseAuthorization)
            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();
            app.MapHub<RetradeBE.Hubs.AccountHub>("/hubs/accounts");
            app.MapHub<SellerHub>("/hubs/sellers");
            app.MapHub<OrderHub>("/hubs/orders");
            app.MapHub<AuctionHub>("/hubs/auctions");

            app.Run();
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
                "Seller",
                49000m,
                30,
                "Activate the right to create discount codes. Freely distribute vouchers for the shop. Attract more customers.");

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

            if (!dbContext.Category.Any(c => c.CategoryId == "cat_20260701_100001"))
            {
                dbContext.Category.Add(new Category
                {
                    CategoryId = "cat_20260701_100001",
                    Name = "Demo Electronics",
                    Description = "Seed data for testing order list before checkout is available.",
                    Status = "Active",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            SeedDemoProduct(
                dbContext,
                "prd_20260701_100001",
                "img_20260701_100001",
                "Iphone 13 pro max 256gb - 99%",
                "Second-hand phone used for testing order list.",
                1250000m,
                "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=600&auto=format&fit=crop&q=80");

            SeedDemoProduct(
                dbContext,
                "prd_20260701_100002",
                "img_20260701_100002",
                "Tai nghe sony wh-1000xm4 - likenew",
                "Tai nghe không dây chính hãng giá tốt, còn bảo hành 6 tháng.",
                650000m,
                "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=600&auto=format&fit=crop&q=80");

            
            SeedDemoProduct(
                dbContext,
                "prd_20260701_100003",
                "img_20260701_100003",
                "Test auction",
                "Sản phẩm test đấu giá.",
                null,
                "https://data.lvo.vn/media/upload/1001210/Image/auction-hammer.jpg",
                "Ready",
                1);

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
            string status = "Accepted",
            int stockQuantity = 5)
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
                    CategoryId = "cat_20260701_100001",
                    Name = name,
                    Description = description,
                    Condition = "Used",
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
