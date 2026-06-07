# ReTrade Backend Service

ReTrade Backend là dịch vụ API cốt lõi cho nền tảng ReTrade — một ứng dụng thương mại điện tử giao dịch và đấu giá đồ cũ. Được xây dựng trên nền tảng .NET 8, hệ thống cung cấp các endpoint RESTful hiệu năng cao và bảo mật để phục vụ ứng dụng ReTrade frontend.

## Công nghệ sử dụng

- Framework: ASP.NET Core 8.0 Web API
- ORM: Entity Framework Core (EF Core 8)
- Cơ sở dữ liệu: PostgreSQL (sử dụng thư viện Npgsql.EntityFrameworkCore.PostgreSQL)
- Tài liệu API: Swagger (Swashbuckle.AspNetCore)

---

## Các mô-đun nghiệp vụ chính

Hệ thống backend quản lý và phân quyền các tài nguyên sau:

- Xác thực & Phân quyền: Đăng nhập thường, Đăng nhập qua Google OAuth2, Đăng ký xác thực qua mã OTP lưu Cache, Đổi mật khẩu tiêu chuẩn qua `/change-password`.
- Quản lý sản phẩm: Đăng sản phẩm, phân mục sản phẩm (Categories) và các thuộc tính liên quan.
- Phiên đấu giá: Tạo phòng đấu giá trực tiếp, ghi nhận lượt đặt giá (Bids), và tiền cọc đấu giá để tránh bùng hàng.
- Đơn hàng & Thanh toán: Xử lý đơn hàng, theo dõi lịch sử mua sắm và yêu cầu hoàn tiền.
- Thông báo & Chat: Hệ thống thông báo tự động khi có biến động về giá hoặc đấu giá.

---

## Cập nhật luồng đổi mật khẩu (Password Update Details)

Để đơn giản hóa và tăng tính bảo mật cho hệ thống, endpoint đặc thù `change-password-after-recovery` đã bị gỡ bỏ hoàn toàn:
- Hệ thống quy về sử dụng duy nhất API đổi mật khẩu tiêu chuẩn: `POST /api/Account/change-password` thông qua hàm `ChangePasswordAsync`.
- API này yêu cầu người dùng điền đầy đủ mật khẩu cũ (hoặc mật khẩu tạm thời nhận qua email khi khôi phục) và mật khẩu mới để tăng tính an toàn tối đa.
- DTO sử dụng: `ChangePasswordDto` (chứa `OldPassword` và `NewPassword`).
- Class DTO dư thừa `ChangePasswordAfterRecoveryDto` đã bị xóa bỏ hoàn toàn.

---

## Yêu cầu hệ thống

Trước khi bắt đầu chạy backend, hãy chuẩn bị:
- Cài đặt .NET 8 SDK
- Khởi chạy PostgreSQL trên cổng mặc định 5432 và tạo cơ sở dữ liệu trống.

---

## Hướng dẫn thiết lập cơ sở dữ liệu và vận hành

### 1. Cấu hình chuỗi kết nối (Connection String)
Cập nhật chuỗi kết nối cơ sở dữ liệu của bạn trong tệp `appsettings.json` tại khóa `DefaultConnection`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=retrade;Username=postgres;Password=YourPassword123!;"
}
```

### 2. Cập nhật Cơ sở dữ liệu (Migrations)
Để tạo một bản di chuyển (migration) mới sau khi thay đổi code Model của Entity Framework:
```bash
dotnet ef migrations add <MigrationName>
```
Để áp dụng các file migration vào PostgreSQL và cập nhật cấu trúc bảng:
```bash
dotnet ef database update
```

### 3. Tạo Entity Model tự động từ DB (Database First Scaffold)
Nếu bạn thay đổi cấu trúc bảng trực tiếp trên PostgreSQL và muốn tạo ngược lại các file code C# Model trong project:
```bash
dotnet ef dbcontext scaffold Name=ConnectionStrings:DefaultConnection Npgsql.EntityFrameworkCore.PostgreSQL -o Models --context-dir Data --context AppDbContext --force --no-pluralize
```

### 4. Chạy dịch vụ Backend
Di chuyển vào thư mục chứa code `RetradeBE` và khởi chạy API:
```bash
dotnet run
```
Bạn có thể sử dụng lệnh `dotnet watch run` để tự động tải lại code (hot reload) mỗi khi thực hiện thay đổi file.

### 5. Xem tài liệu tích hợp (Swagger API UI)
Sau khi API khởi chạy thành công, truy cập đường dẫn sau trên trình duyệt để kiểm tra và test thử các endpoint:
- http://localhost:<port>/swagger
- https://localhost:<port>/swagger
