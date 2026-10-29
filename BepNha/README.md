# Bếp Nhà – Cộng đồng chia sẻ công thức nấu ăn

Solution ASP.NET Core MVC có giao diện Razor, xử lý nghiệp vụ phía máy chủ và dữ liệu quan hệ SQL Server.

Tên website: **Bếp Nhà**. Thông điệp: **Công thức hay, bữa cơm ấm**. Trang chủ tập trung tìm công thức theo nguyên liệu, giới thiệu công thức và tác giả từ dữ liệu thật, kết nối cộng đồng yêu bếp.

## Mở và chạy với Visual Studio

1. Cài **Visual Studio 2026 18.0 trở lên**, workload **ASP.NET and web development**, **.NET 10 SDK** và **SQL Server Express LocalDB** (Individual components). Có thể dùng .NET 10 SDK với Visual Studio Code hoặc dòng lệnh .NET.
2. Mở **CookingCommunity.sln**.
3. Chọn cấu hình chạy **CookingCommunity.Web**. Nếu Visual Studio chưa tự chọn, bấm phải project **CookingCommunity.Web** → **Set as Startup Project**.
4. Nhấn **F5** hoặc **Ctrl+F5**, dùng profile **http**.
5. Trình duyệt mở **http://localhost:5016**. Lần đầu ứng dụng tự tạo database **CookingCommunity** cùng đủ 7 bảng và dữ liệu demo.

Connection string nằm ở `appsettings.json`. Cấu hình mặc định dùng Windows Authentication với `(localdb)\MSSQLLocalDB`. Nếu dùng SQL Server khác, đổi `ConnectionStrings:CookingDatabase` trước khi chạy; tài khoản SQL cần quyền tạo database hoặc database trống có quyền tạo bảng.

**Nền tảng:** Ứng dụng nhắm **.NET 10** và dùng **Entity Framework Core 10** với SQL Server. Theo yêu cầu phiên bản của Microsoft, cần Visual Studio 2026 18.0 trở lên để phát triển bằng Visual Studio; Visual Studio 2022 không hỗ trợ target .NET 10. Có thể build và chạy bằng .NET 10 SDK trên dòng lệnh.

## Tài khoản demo

Các tài khoản cùng mật khẩu **BepViet123!**:

| Vai trò | Email |
|---|---|
| Quản trị viên | adminbepnha@gmail.com |
| Thành viên Mộng Thu |thuntm060584@tvu-onschool.edu.vn | Thu12345

Có 10 công thức với nguyên liệu và hướng dẫn đầy đủ, 5 danh mục, đánh giá và quan hệ theo dõi được lưu trong SQL Server. Dữ liệu này chỉ khởi tạo khi bảng người dùng trống, không ghi đè dữ liệu đã có. Tắt `Database:SeedDemo` nếu muốn bắt đầu với database trống; khi đó cần tạo tài khoản admin và danh mục bằng SQL trước khi có thể đăng công thức. Đây là dữ liệu demo nghiệm thu, không thay thế truy vấn hoặc nghiệp vụ thật.

## Cấu trúc

- `CookingCommunity.sln` và `CookingCommunity.Web.csproj` nằm ngay tại thư mục gốc; solution chỉ có một project MVC.
- `Controllers`: xử lý request và nghiệp vụ MVC.
- `Models`: entity dữ liệu, model biểu mẫu và model hiển thị.
- `Data`: `CookingDbContext`, cấu hình SQL Server, khởi tạo dữ liệu và nâng cấp schema.
- `Services`: xác thực phiên đăng nhập và lưu ảnh tải lên.
- `Views` và `wwwroot`: Razor, CSS, JavaScript và hình ảnh giao diện.
- `Data/schema.sql`: cấu trúc 7 bảng SQL Server; `Data/upgrade-soft-delete.sql`: nâng cấp an toàn cho database cũ.
- Các cột ngày giờ dùng kiểu SQL Server `datetime`, ánh xạ từ `DateTime`/`DateTime?` trong C#. `Data/upgrade-datetime.sql` tự chạy khi khởi động để chuyển các cột ngày giờ của database cũ sang `datetime`, giữ tính cho phép NULL và chỉ mục. Giá trị phần giây được làm tròn theo độ chính xác của SQL Server `datetime`; nếu có ngày ngoài phạm vi hỗ trợ thì toàn bộ bước chuyển bị hủy để giữ dữ liệu gốc.

Database được khởi tạo bằng **EnsureCreated**, không dùng bảng lịch sử migration để giữ đúng **7 bảng** theo PRD. Không chạy `schema.sql` lần nữa nếu ứng dụng đã tạo bảng. Với database đã có dữ liệu, thay đổi cấu trúc cần script nâng cấp riêng; không tự xóa hoặc tạo lại database.

## Build và chạy

Từ thư mục Solution, chạy:

```powershell
dotnet restore CookingCommunity.sln
dotnet build CookingCommunity.sln -c Release
dotnet run --project CookingCommunity.Web.csproj --launch-profile http
```

Ứng dụng dùng SQL Server LocalDB và mở tại **http://localhost:5016** theo profile `http`. SDK được chọn trong `global.json` thuộc nhánh .NET 10.0; cho phép sử dụng feature band mới hơn của cùng nhánh khi đã cài đặt.

## Nghiệp vụ và bảo mật

- Công thức mới hiện ngay, phân trang 12/trang; tìm theo tên hoặc nguyên liệu dùng chung, lọc danh mục/độ khó, sắp xếp mới nhất/điểm cao.
- Thành viên chỉ sửa/xóa công thức của mình. Công thức bị ẩn vẫn sửa được nhưng chỉ admin bật lại.
- Một đánh giá 1–5 sao mỗi người/công thức, cập nhật/xóa bằng AJAX; không tự đánh giá, không tự theo dõi.
- Đặt lại mật khẩu hiển thị liên kết trên màn hình, không gửi email; token ngẫu nhiên 256 bit, chỉ lưu SHA-256 trong SQL, không giới hạn thời gian và chỉ dùng một lần. Yêu cầu mới thay liên kết cũ.
- BCrypt work factor 12; mật khẩu ít nhất 8 ký tự có chữ và số; giới hạn 72 byte UTF-8 theo BCrypt.
- Cookie HttpOnly, SameSite Lax; kiểm tra trạng thái khóa và dấu vết mật khẩu mỗi yêu cầu. Đổi/đặt lại mật khẩu vô hiệu các phiên cũ.
- Tất cả thao tác thay đổi dùng POST và antiforgery; kiểm tra quyền trên server, Razor encode dữ liệu, EF Core tham số hóa truy vấn.
- Upload JPG (`.jpg`) hoặc PNG (`.png`) tối đa 5 MB, kiểm tra chữ ký tệp và đặt tên GUID trong `wwwroot/uploads`.
- Lưu công thức/nguyên liệu trong transaction; xóa công thức cascade nguyên liệu liên kết/đánh giá, giữ danh mục nguyên liệu dùng chung.
- Không cho xóa danh mục đang có công thức hoặc admin khóa chính mình. Lý do ẩn bắt buộc 5–300 ký tự.

## Giao diện và thư viện

Tài nguyên giao diện được quản lý trong `wwwroot/assets`; bố cục từng trang được hiện thực qua Razor layout, view và các thành phần dùng chung. CSS của ứng dụng nằm trong `wwwroot/css/site.css` và `wwwroot/css/admin.css`; JavaScript xử lý tương tác trình duyệt trong `wwwroot/js/site.js`. Các thư viện và tài nguyên đi kèm cần được sử dụng theo điều kiện cấp phép tương ứng.

Bộ ảnh bìa của 10 công thức khởi tạo nằm trong `wwwroot/assets/img/recipes`, được tạo riêng theo tên và nguyên liệu từng món. Nội dung yêu cầu tạo ảnh được ghi trong `Data/recipe-image-prompts.md`. Script `Data/upgrade-recipe-images.sql` tự thay đường dẫn ảnh có sẵn của database cũ khi khởi động; ảnh do thành viên tự tải lên được giữ nguyên.

Profile local dùng HTTP để mở chạy trực tiếp. Khi triển khai máy chủ thật, cấu hình HTTPS, connection string riêng, tắt demo và đổi mật khẩu demo.
