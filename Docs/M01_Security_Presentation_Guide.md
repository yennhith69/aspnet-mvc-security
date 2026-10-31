# NEU-SecHRM - Hướng dẫn slide báo cáo

## Cách chụp ảnh

Chụp cửa sổ trình duyệt và SSMS, che email/IP/tài khoản thật trước khi đưa vào slide.
Mỗi ảnh nên có URL hoặc tiêu đề màn hình để chứng minh ngữ cảnh. Không chụp mật khẩu,
JWT key, connection string đầy đủ hoặc private key chứng chỉ.

## Kịch bản slide

| Slide | Nội dung nói | Ảnh/bằng chứng cần chụp |
|---|---|---|
| 1 | NEU-SecHRM quản lý và bảo vệ hồ sơ cán bộ/giảng viên. | Trang đăng nhập và logo hệ thống |
| 2 | Quy trình M01: tiếp nhận, kiểm tra trùng, cấp mã, xác nhận và cập nhật. | Danh sách hồ sơ và chi tiết hồ sơ |
| 3 | Các vai trò được giới hạn theo nghiệp vụ. | Màn hình tài khoản/quyền và menu sidebar |
| 4 | Mục 1 - đặc quyền tối thiểu: ứng dụng không dùng tài khoản sysadmin. | `001_LoginsRoles.sql` và kết quả role |
| 5 | Mục 2 - bảng gốc được bảo vệ, báo cáo dùng view/procedure. | `002_ViewsProcedures.sql`, kết quả GRANT |
| 6 | Mục 3 - RLS lọc theo vai trò, đơn vị và hồ sơ cá nhân. | `004_RowLevelSecurity.sql` và hai kết quả SELECT |
| 7 | Mục 4 - DDM che PII khi không có UNMASK. | Kết quả SELECT masked/unmasked |
| 8 | Mục 5 - SQL Audit ghi nhận đăng nhập và thao tác nhạy cảm. | `005_SqlServerAudit.sql` và audit log |
| 9 | Mục 6 - TDE bảo vệ dữ liệu at rest. | `006_TDE.sql` và `sys.dm_database_encryption_keys` |
| 10 | Mục 7 - backup được mã hóa và restore được xác minh. | `007_BackupRestore.sql`, VERIFYONLY và restore test |
| 11 | Mục 8 - theo dõi đăng nhập thất bại và thành viên sysadmin. | `008_AccountMonitoring.sql` và LoginLogs |
| 12 | Mục 9 - Identity khóa sau 5 lần sai trong 15 phút. | Màn hình lỗi khóa tài khoản và LoginLogs |
| 13 | Mục 10 - EF Core LINQ và tham số hóa loại bỏ SQL injection. | Form tìm kiếm và log/query parameter |
| 14 | Mục 11 - Razor encoding và CSP không cho inline script/style. | DevTools Response Headers |
| 15 | Mục 12 - CSRF toàn cục, cookie Secure/Strict. | Form POST có token và kết quả thiếu token bị 400 |
| 16 | Mục 13 - JWT key lấy từ User Secrets/environment. | Cấu hình đã che secret, không chụp giá trị |
| 17 | Mục 14 - upload whitelist, magic bytes, GUID và ngoài wwwroot. | Màn hình thêm minh chứng và thư mục App_Data |
| 18 | Mục 15 - log thao tác nhưng không ghi mật khẩu. | LoginLogs/AuditLogs |
| 19 | Mục 16 - RBAC đọc quyền từ database. | Bảng Quyen/VaiTroQuyen và menu theo vai trò |
| 20 | Mục 17 - đổi mật khẩu và lưu lịch sử hash. | Màn hình đổi mật khẩu và LichSuMatKhaus |
| 21 | Mục 18 - scope PII theo HR/chính chủ/đơn vị. | Cùng hồ sơ ở HR và Lecturer |
| 22 | Mục 19 - tìm kiếm, lọc, phân trang, sort whitelist. | Màn hình Hồ sơ nhân sự |
| 23 | Mục 20 - audit hai tầng ứng dụng và SQL Server. | AuditLogs cùng SQL Audit file |
| 24 | Mục 21 - dữ liệu lương được hạn chế theo quyền. | Màn hình Nâng lương với dữ liệu phù hợp quyền |
| 25 | Mục 22 - API JWT, rate limit và lockout. | API login/API hồ sơ, phản hồi 401/429 |
| 26 | Mục 23 - lỗi không lộ stack trace. | Trang Error và response production |
| 27 | Mục 24 - retry, health check và restore. | `/health`, `EnableRetryOnFailure`, restore output |
| 28 | Mục 25 - rà soát mã nguồn và cấu hình. | Bảng checklist trong báo cáo bảo mật |
| 29 | Mục 26 - rate limit và cache danh mục. | Code rate limit/cache và response 429 |
| 30 | Mục 27 - đo execution plan và index. | `SET STATISTICS IO/TIME`, plan trước/sau |
| 31 | Mục 28 - kiểm soát package và CVE. | `dotnet list package --vulnerable` |
| 32 | Mục 29 - HTTPS, Secure cookie và cấu hình thư viện. | HTTPS URL và header/cookie DevTools |
| 33 | Mục 30 - bốn ca kiểm thử upload. | Bảng kết quả M30 và ảnh lỗi từng ca |
| 34 | Kết luận: bảo mật được gắn vào workflow, không chỉ nằm trong tài liệu. | Dashboard M01 và bảng tổng hợp kết quả |

## Bằng chứng nghiệp vụ bổ sung

- F9: chụp màn hình bộ lọc đồng thời theo từ khóa, đơn vị, tình trạng công tác và trạng thái hồ sơ.
- F11: chụp màn hình `Báo cáo`, hộp thoại in và file CSV mở bằng Excel.
- F7: chụp màn hình `Khen thưởng`, `Kỷ luật` và lịch sử nâng lương sau khi HR lưu dữ liệu.
- F5: chụp màn hình xóa trình độ và minh chứng; xác nhận file lưu ngoài `wwwroot` đã được xóa.
- CSDL: chạy `Database/028_Add_KyLuat.sql`, sau đó chụp `SELECT TOP (20) * FROM dbo.KyLuats`.

## Câu kết

“NEU-SecHRM tách quyền theo vai trò và phạm vi hồ sơ, bảo vệ dữ liệu ở cả ứng dụng
và SQL Server, đồng thời mỗi tiêu chí đều có chức năng hoặc bằng chứng kiểm thử để
đối chiếu trong quá trình nghiệm thu.”
