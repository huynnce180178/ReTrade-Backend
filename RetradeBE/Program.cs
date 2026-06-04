
using Microsoft.EntityFrameworkCore;
// using RetradeBE.Data;

namespace RetradeBE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            // builder.Services.AddDbContext<AppDbContext>(options =>
            //     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Lấy đường dẫn Frontend từ appsettings.json
            var frontendUrl = builder.Configuration.GetValue<string>("FrontendUrl") ?? "http://localhost:5173";

            // Thêm CORS để Frontend có thể gọi API (ví dụ: React, Vue, Angular chạy ở port khác)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy =>
                    {
                        policy.WithOrigins(frontendUrl)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Kích hoạt CORS (Phải đặt trước UseAuthorization)
            app.UseCors("AllowFrontend");

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
