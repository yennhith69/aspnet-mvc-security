# M25 - Báo cáo rà soát bảo mật

| Nhóm | Trạng thái | Bằng chứng |
|---|---|---|
| Identity/RBAC/scope | Đã triển khai | `Program.cs`, `PermissionAuthorization.cs`, `HoSoScopeService.cs` |
| SQL Server security | Đã có script | `Database/001` đến `Database/011` |
| CSRF/XSS/CSP | Đã triển khai | `Program.cs`, Razor forms, response headers |
| Upload | Đã triển khai | `TrinhDoController.cs`, `M30_Upload_TestCases.md` |
| Availability | Đã triển khai | retry, `/health`, backup/restore scripts |
| M01 workflows | Đã triển khai | `M01Controller.cs`, `TrinhDoController.cs`, `HopDongController.cs`, `KyLuat` migration |
| F11 báo cáo | Đã triển khai | In trình duyệt và xuất CSV tại `M01/BaoCao` |
| Tra cứu nâng cao | Đã triển khai | Lọc từ khóa, đơn vị, tình trạng, trạng thái, phân trang và sort whitelist |
| Kỷ luật | Đã triển khai | `KyLuat` entity, workflow HR và `Database/028_Add_KyLuat.sql` |

Các script SQL cần được chạy và lưu output SSMS trong hồ sơ nghiệm thu. Không đưa
secret, private key hoặc dữ liệu cá nhân thật vào ảnh/slides.
