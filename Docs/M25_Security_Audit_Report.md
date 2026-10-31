# M25 - Báo cáo rà soát bảo mật

## Phạm vi

Rà soát mã nguồn ASP.NET Core MVC, cấu hình Identity/JWT, truy vấn EF Core,
upload minh chứng và các script bảo vệ SQL Server.

## Kết quả khắc phục

| Hạng mục | Bằng chứng triển khai | Rủi ro còn lại |
|---|---|---|
| XSS/CSP | CSP chỉ cho phép script/style từ `'self'`; Razor encoding được giữ nguyên | Cần rà soát CSP theo từng CDN nếu bổ sung tài nguyên ngoài |
| CSRF | `AutoValidateAntiforgeryTokenAttribute`, cookie Strict | API JWT không dùng cookie |
| SQL Injection | EF Core LINQ và tham số hóa stored procedure | Không dùng SQL động từ dữ liệu người dùng |
| PII/RBAC | `HoSoScopeService`, policy quyền đọc từ DB, masking số giấy tờ/lương | Quyền SQL Server phải triển khai đúng các script Database |
| Upload | Extension whitelist, magic bytes, giới hạn dung lượng, tên GUID, lưu ngoài `wwwroot` | Cần chạy đầy đủ bộ test M30 trước release |
| JWT/Identity | Secret ngoài mã nguồn, lockout 5 lần/15 phút, HTTPS metadata bắt buộc | Không dùng secret mặc định trong môi trường triển khai |
| CSDL | Retry khi lỗi tạm thời, health check `/health`, TDE/Audit/RLS/DDM scripts | Cần giám sát và kiểm thử restore định kỳ |
| DoS/hiệu năng | Rate limit, giới hạn kết quả, cache danh mục, index và script đo lường M27 | Cần theo dõi metric production |

## Khuyến nghị vận hành

1. Dùng User Secrets/secret store cho JWT và mật khẩu seed.
2. Không sử dụng `TrustServerCertificate=True` trong production; triển khai chứng chỉ SQL Server hợp lệ.
3. Chạy `Database/027_Query_Performance_Test.sql` trên dữ liệu đại diện và lưu kết quả execution plan.
4. Chạy các test upload trong `Docs/M30_Upload_TestCases.md` ở CI hoặc staging.
