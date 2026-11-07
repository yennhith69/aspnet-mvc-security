# NEU-SecHRM

Hệ thống **NEU-SecHRM** là ứng dụng ASP.NET Core MVC quản lý hồ sơ cán bộ và giảng
viên cho module M01. Dự án tập trung vào việc bảo vệ dữ liệu nhân sự bằng nhiều lớp:
Identity, phân quyền nghiệp vụ, giới hạn phạm vi dữ liệu, bảo vệ ở SQL Server, masking,
audit, chống tấn công web và bảo vệ dữ liệu sao lưu.

## Chức năng chính

- Đăng nhập, quản lý phiên và khóa tài khoản khi đăng nhập sai nhiều lần.
- Phân quyền theo vai trò: HR, Manager, Lecturer và Administrator.
- Tiếp nhận hồ sơ, cấp mã, cập nhật và xác nhận hồ sơ.
- Quản lý thông tin cá nhân, giấy tờ, hợp đồng và quá trình công tác.
- Quản lý trình độ, chứng chỉ, tệp minh chứng, lương, khen thưởng, kỷ luật,
  giờ giảng và hoạt động nghiên cứu khoa học.
- Tra cứu, báo cáo, xuất dữ liệu và API xác thực bằng JWT.
- Ghi nhật ký các thao tác nghiệp vụ và audit ở tầng SQL Server.

## Công nghệ sử dụng

- .NET 10 / ASP.NET Core MVC
- Entity Framework Core 10
- ASP.NET Core Identity
- SQL Server
- JWT Bearer Authentication
- Razor Views, HTML, CSS và JavaScript
- ClosedXML cho Excel
- QuestPDF cho báo cáo PDF

## Yêu cầu môi trường

- .NET SDK 10
- SQL Server hoặc SQL Server Express
- Visual Studio 2022 phiên bản hỗ trợ .NET 10, hoặc VS Code có C# Dev Kit
- PowerShell

## Cấu hình cục bộ

### 1. Cấu hình cơ sở dữ liệu

Mặc định, ứng dụng đọc connection string `ConnectionStrings:Default` trong
`appsettings.json`. Có thể thay đổi cho môi trường cá nhân trong
`appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost\\SQLEXPRESS;Database=HrmSecure;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Không commit mật khẩu, connection string production hoặc thông tin nhạy cảm vào
repository. Với SQL Authentication, hãy dùng User Secrets hoặc biến môi trường.

### 2. Cấu hình JWT bằng User Secrets

Ứng dụng yêu cầu `Jwt:Key` có tối thiểu 32 byte. Tại thư mục chứa file
`mvcSecurity.csproj`, chạy:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<chuoi-ngau-nhien-it-nhat-32-byte>"
dotnet user-secrets set "Jwt:Issuer" "NEU-SecHRM"
dotnet user-secrets set "Jwt:Audience" "NEU-SecHRM-Client"
```

Không ghi khóa JWT trực tiếp vào mã nguồn hoặc file cấu hình được commit.

## Khởi tạo cơ sở dữ liệu

1. Tạo database `HrmSecure` trên SQL Server.
2. Chạy các script trong thư mục [`Database`](Database) bằng tài khoản quản trị
   SQL Server theo thứ tự số:

   - `001_LoginsRoles.sql`: database role, login và quyền tối thiểu.
   - `002_ViewsProcedures.sql`: view và stored procedure.
   - `003_Masking.sql`: dynamic data masking.
   - `004_RowLevelSecurity.sql`: hàm và policy RLS.
   - `005_SqlServerAudit.sql`: SQL Server Audit.
   - `006_TDE.sql`: TDE cho database.
   - `007_BackupRestore.sql`: backup mã hóa và kiểm tra restore.
   - Các script bổ sung như `028_Add_KyLuat.sql` và `029_RlsNewTables.sql`:
     chạy sau khi các bảng tương ứng đã tồn tại.

3. Nếu chỉ chạy môi trường phát triển, có thể dùng EF Core migrations:

```powershell
dotnet ef database update
```

Các script liên quan đến login, role, RLS, masking, audit, TDE và backup cần được
chạy bằng tài khoản có quyền quản trị SQL Server; không nên chạy chúng bằng tài khoản
ứng dụng `hrm_app`.

## Chạy ứng dụng

Khôi phục package và khởi động ứng dụng:

```powershell
dotnet restore
dotnet build
dotnet run
```

Mở URL HTTPS được hiển thị trong terminal, thường có dạng:

```text
https://localhost:7236
```

Endpoint kiểm tra trạng thái:

```text
https://localhost:7236/health
```

Endpoint `/health` chỉ trả trạng thái tổng quát, không trả connection string hoặc chi
tiết lỗi database.

## Mô hình bảo mật

### Xác thực và phân quyền

- ASP.NET Core Identity quản lý mật khẩu, lockout và cookie phiên.
- Cookie có `HttpOnly`, `Secure` và `SameSite=Strict`.
- Policy authorization kiểm tra quyền ở server bằng `PermissionHandler`.
- Fallback policy yêu cầu xác thực đối với các endpoint không được đánh dấu công khai.
- JWT được dùng cho API và được kiểm tra issuer, audience, thời hạn và chữ ký.

### Bảo vệ dữ liệu

- RLS lọc dữ liệu theo vai trò, đơn vị và mã nhân sự thông qua `SESSION_CONTEXT`.
- `pol_NhanSu` bảo vệ bảng nhân sự chính.
- `pol_BangCon` bảo vệ các bảng con như giấy tờ, lương, sức khỏe, gia đình và
  kỷ luật.
- Dynamic Data Masking che các trường nhạy cảm.
- TDE bảo vệ database trên thiết bị lưu trữ.
- Backup được mã hóa, kiểm tra checksum và có quy trình xác minh/restore.

### Bảo vệ ứng dụng web

- Antiforgery token được bật cho các form POST.
- Razor output encoding và Content Security Policy giảm nguy cơ XSS.
- Security headers chống clickjacking, MIME sniffing và hạn chế referrer.
- Upload kiểm tra phần mở rộng, magic bytes, dung lượng và lưu file ngoài `wwwroot`
  bằng tên GUID.
- Rate limit theo IP trả `429 Too Many Requests` khi vượt ngưỡng.
- EF Core dùng `EnableRetryOnFailure`; transaction tiếp nhận hồ sơ chạy trong
  execution strategy.
- `/health` kiểm tra khả năng kết nối database.

### Audit và bảo toàn lịch sử

- `AuditLogs` ghi người dùng, hành động, đối tượng, mã đối tượng và IP.
- SQL Server Audit ghi độc lập các hoạt động quan trọng ở tầng database.
- Dữ liệu lịch sử lương và kỷ luật không có thao tác xóa cứng trên giao diện.
- Các thao tác xóa được cho phép ở bảng phụ phải được kiểm tra quyền và ghi audit.

## Cấu trúc thư mục

```text
Controllers/     Controller MVC và endpoint nghiệp vụ
Data/             DbContext, seed dữ liệu và interceptor
Database/         Script role, RLS, masking, audit, TDE và backup
Docs/             Tài liệu hướng dẫn và báo cáo bảo mật
Migrations/       EF Core migrations
Models/           Entity và model nghiệp vụ
Services/         Authorization, health check và dịch vụ dùng chung
Views/            Razor views
wwwroot/          CSS, JavaScript và tài nguyên tĩnh
Program.cs        Cấu hình ứng dụng, middleware và pipeline
appsettings*.json Cấu hình theo môi trường, không chứa secret production
```

## Kiểm tra trước khi phát hành

```powershell
dotnet build .\mvcSecurity.csproj --no-restore
git diff --check
dotnet list .\mvcSecurity.csproj package --vulnerable
```

Ngoài kiểm tra build, cần xác minh bằng tài khoản thử nghiệm:

- Người chưa đăng nhập không thấy menu nội bộ.
- HR, Manager, Lecturer và Administrator chỉ thấy đúng phạm vi.
- RLS lọc đúng dữ liệu ở cả bảng chính và bảng con.
- Masking che đúng trường khi tài khoản không có `UNMASK`.
- API trả `401/403` khi token không hợp lệ hoặc không đủ quyền.
- Rate limit trả `429` khi vượt ngưỡng.
- Upload từ chối file sai loại, file giả mạo và file vượt dung lượng.
- Audit xuất hiện sau thao tác nghiệp vụ quan trọng.
- Backup có thể `VERIFYONLY` và restore trong môi trường kiểm thử.

## Lưu ý an toàn

- Không commit secret, mật khẩu, JWT key, certificate hoặc backup chứa dữ liệu thật.
- Không dùng tài khoản DBA để chạy ứng dụng.
- Chỉ dùng dữ liệu mẫu hoặc dữ liệu đã ẩn danh trong môi trường phát triển.
- Các script thay đổi quyền database cần được xem xét trước khi chạy trên production.
- Khi triển khai production, bật HTTPS hợp lệ, HSTS và quy trình backup/restore định kỳ.

## Giấy phép

Đây là dự án học tập phục vụ môn Bảo mật phần mềm và cơ sở dữ liệu.
