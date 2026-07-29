# Quy tắc viết Unit Test / Unit Testing Rules

Tài liệu này định nghĩa các quy tắc và cấu trúc để viết Unit Test cho dự án, giúp các thành viên và các AI coding assistant sau này tuân thủ thống nhất.

## 1. Cấu trúc thư mục & File Test (Directory & File Structure)
- **Tập trung vào Service Layer:** Các Unit Test tập trung kiểm thử các nghiệp vụ tại tầng Service.
- **Mỗi Method một file test riêng biệt:** Mỗi method trong Service sẽ được viết test case trong một file test riêng biệt để dễ quản lý.
- **Phân chia theo thư mục Entity:** Các file test được tổ chức trong thư mục tương ứng với Entity của Service đó dưới thư mục `Test`.
  - Cấu trúc: `Test/{EntityName}/{EntityName}{MethodNameWithoutAsync}Tests.cs`
  - Ví dụ:
    - Method `ReportReviewAsync` của `ReportService` -> File `Test/Report/ReportReportReviewTests.cs`
    - Method `HideForReportAsync` của `ReviewService` -> File `Test/Review/ReviewHideForReportTests.cs`

---

## 2. Quy tắc đặt tên (Naming Conventions)
- **Namespace:** Sử dụng `Test.{EntityName}Tests` hoặc `Test.{EntityName}` (ví dụ: `Test.ReportTests`).
- **Tên Test Class:** Đặt theo tên file `{EntityName}{MethodNameWithoutAsync}Tests`.
- **Tên Test Method:** Đặt tên rõ nghĩa thể hiện hành vi được test và kết quả mong muốn.
  - Định dạng: `[TênMethod]_[KếtQuảMongMuốn]_[KhiĐiềuKiệnXảyRa]`
  - Ví dụ: `ReportReviewAsync_ShouldThrowUnauthorizedAccessException_WhenAccountNotFound`

---

## 3. Khởi tạo AutoMapper trong Unit Test (AutoMapper Configuration)
- Khi viết các unit test cần dùng mapper thực tế (ví dụ: test các method có dùng `ProjectTo`), cần khởi tạo `MapperConfiguration`.
- **Lưu ý quan trọng cho AutoMapper 15.0+:** Constructor của `MapperConfiguration` yêu cầu tham số thứ hai là `ILoggerFactory`.
- Luôn truyền `Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance` vào làm tham số thứ hai để tránh lỗi `ArgumentNullException: Value cannot be null (Parameter 'logger')` do trình kiểm tra bản quyền (license validator) nội bộ của AutoMapper gây ra:
  ```csharp
  var configuration = new AutoMapper.MapperConfiguration(cfg =>
  {
      cfg.AddProfile<AutoMapperProfile>();
  }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
  _mapper = configuration.CreateMapper();
  ```

---

## 4. Ràng buộc thực thi đối với AI Assistant (Execution Constraint)
- **Chỉ tạo Test Case, không tự chạy test:** AI Assistant chỉ có nhiệm vụ tạo và cập nhật mã nguồn test case. Tuyệt đối **không** tự ý chạy các lệnh test (như `dotnet test`) để kiểm tra thử trừ khi được người dùng yêu cầu trực tiếp.

---

## 5. Tiêu chí lựa chọn Test Case (Test Case Selection Criteria)
- **Đầy đủ các phân nhóm test (N - A - B):** Đảm bảo viết đủ 3 loại test case cho mỗi method:
  - **N (Normal):** Các trường hợp chạy bình thường, dữ liệu hợp lệ và kết quả thành công mong muốn.
  - **A (Abnormal):** Các trường hợp lỗi, ngoại lệ, dữ liệu không hợp lệ (ví dụ: null, rỗng, không đúng định dạng) và kiểm tra xem hệ thống có ném ra ngoại lệ/lỗi tương ứng hay không.
  - **B (Boundary):** Các trường hợp cận biên (ví dụ: giá trị tối thiểu/tối đa, độ dài cận biên, các giới hạn điều kiện logic).
- **Tối ưu & Ràng buộc số lượng test case:** Giới hạn và tối ưu số lượng test case vừa đủ để đạt độ bao quát (coverage) cao nhất mà không làm phình to file test vô ích. Tuy nhiên, số lượng test case viết cho mỗi method phải đáp ứng công thức so với số dòng code (Line of Code - LoC) của method đó:
  `=IF(100<>"N/A";SUM(lineCode*100/1000;-totalTestCase);"N/A")` (Số lượng test case phải >= Line Code).

