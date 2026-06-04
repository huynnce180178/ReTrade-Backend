# ReTrade Backend

ReTrade Backend là dịch vụ API cốt lõi cho nền tảng ReTrade — một ứng dụng thương mại điện tử toàn diện được thiết kế để giao dịch, đấu giá và khám phá sản phẩm. Được xây dựng với .NET 8, hệ thống cung cấp các endpoint RESTful mạnh mẽ, có khả năng mở rộng và bảo mật cao để vận hành ứng dụng ReTrade frontend.

## 🚀 Công nghệ sử dụng

- **Framework:** ASP.NET Core 8.0 Web API
- **ORM:** Entity Framework Core (EF Core 8)
- **Cơ sở dữ liệu:** PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Tài liệu API:** Swagger (`Swashbuckle.AspNetCore`)

## 📦 Các tính năng chính

Dựa trên các mô hình nghiệp vụ, hệ thống backend hỗ trợ các mô-đun chính sau:

- **Quản lý & Xác thực người dùng:** Tài khoản, Phân quyền, Hồ sơ người dùng, Theo dõi (Follows) và Tìm kiếm.
- **Danh mục sản phẩm:** Sản phẩm, Danh mục, Thuộc tính và Hình ảnh.
- **Đấu giá & Đặt giá:** Phiên đấu giá, Lượt đặt giá, Trả giá (Offers) và Tiền cọc đấu giá.
- **Giao dịch & Đơn hàng:** Đơn hàng, Chi tiết đơn hàng, Thanh toán và Yêu cầu hoàn tiền.
- **Giao tiếp:** Chat theo thời gian thực, Phòng chat và Thông báo.
- **Tương tác của người dùng:** Danh sách yêu thích, Yêu thích, Đánh giá và Mã giảm giá (Voucher).

## 🛠️ Yêu cầu hệ thống

Trước khi bắt đầu, hãy đảm bảo bạn đã cài đặt các phần mềm sau:
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) đã cài đặt và chạy trên cổng mặc định `5432`.

## ⚙️ Cấu hình & Cài đặt

1. **Cấu hình Cơ sở dữ liệu**
   Chuỗi kết nối cơ sở dữ liệu được đặt trong file `appsettings.json`. Cập nhật chuỗi kết nối `DefaultConnection` với thông tin đăng nhập PostgreSQL của bạn nếu cần.
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=retrade;Username=postgres;Password=YourPassword123!;"
   }
   ```

2. **Cập nhật Cơ sở dữ liệu (Migrations)**
   Để tạo một bản di chuyển (migration) mới dựa trên thay đổi của Model, hãy chạy lệnh:
   ```bash
   dotnet ef migrations add <MigrationName>
   ```
   Để áp dụng các bản di chuyển (migrations) và cập nhật cấu trúc cơ sở dữ liệu, hãy chạy lệnh sau trong thư mục dự án:
   ```bash
   dotnet ef database update
   ```

3. **Tạo Model từ Cơ sở dữ liệu (Database First Scaffolding)**
   Để tạo các mô hình Entity Framework Core từ một cơ sở dữ liệu có sẵn, hãy chạy lệnh sau trong thư mục dự án:
   ```bash
   dotnet ef dbcontext scaffold Name=ConnectionStrings:DefaultConnection Npgsql.EntityFrameworkCore.PostgreSQL -o Models --context-dir Data --context AppDbContext --force --no-pluralize
   ```

4. **Chạy ứng dụng**
   Di chuyển vào thư mục `RetradeBE` và chạy dự án:
   ```bash
   dotnet run
   ```
   Ngoài ra, bạn có thể chạy ứng dụng qua Visual Studio hoặc sử dụng `dotnet watch run` để tự động tải lại (hot reloading) trong quá trình phát triển.

5. **Tài liệu API**
   Sau khi ứng dụng đang chạy, bạn có thể khám phá các endpoint API qua giao diện Swagger tại:
   - `http://localhost:<port>/swagger`
   - `https://localhost:<port>/swagger`

## 🔗 Chia sẻ tài nguyên nguồn gốc chéo (CORS)

Backend được cấu hình để chấp nhận các yêu cầu từ frontend đang chạy tại `http://localhost:5173`. Đảm bảo frontend của bạn chạy trên cổng này hoặc cập nhật giá trị `FrontendUrl` trong `appsettings.json`.

## 📂 Cấu trúc dự án

- `Controllers/`: Chứa các endpoint API xử lý các yêu cầu HTTP gửi đến.
- `Models/`: Các lớp thực thể (Entity) đại diện cho các bảng trong cơ sở dữ liệu.
- `Data/`: Chứa `DbContext` và cấu hình của EF Core.
- `appsettings.json`: Các cài đặt cấu hình cho ứng dụng.
- `Program.cs`: Điểm vào (entry point) cho ứng dụng ASP.NET Core, dùng để cấu hình các dịch vụ (services) và middleware.
