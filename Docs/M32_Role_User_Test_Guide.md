# M32 - Hướng dẫn kiểm thử theo vai trò

Tài liệu này dùng để nghiệm thu chức năng và phân quyền của NEU-SecHRM. Thực hiện lần lượt với cả bốn tài khoản mẫu, không dùng việc ẩn nút giao diện làm bằng chứng duy nhất; luôn thử thêm URL trực tiếp.

## 1. Tài khoản kiểm thử

| Vai trò | Tài khoản | Phạm vi dữ liệu |
|---|---|---|
| Admin | `admin@hrm.test` | Quản trị tài khoản, nhật ký đăng nhập và xem hồ sơ |
| HR | `hr@hrm.test` | Quản lý hồ sơ trong phạm vi được cấp |
| Manager | `manager@hrm.test` | Hồ sơ thuộc đơn vị được gán, hiện là Khoa CNTT |
| Lecturer | `lecturer@hrm.test` | Hồ sơ được liên kết với tài khoản, hiện là `GV2026-0001` |

Mật khẩu lấy từ User Secrets, không ghi vào tài liệu:

```text
Seed:AdminPassword
Seed:HrPassword
Seed:ManagerPassword
Seed:LecturerPassword
```

Địa chỉ kiểm thử mặc định:

```text
https://localhost:7236
```

## 2. Bảng quyền chức năng

| Chức năng | Admin | HR | Manager | Lecturer |
|---|---:|---:|---:|---:|
| Đăng nhập, đổi mật khẩu, đăng xuất | Có | Có | Có | Có |
| Xem danh sách và chi tiết hồ sơ | Có | Có | Có, theo đơn vị | Có, theo hồ sơ |
| Tìm kiếm, lọc, phân trang | Có | Có | Có, trong phạm vi | Có, trong phạm vi |
| Tiếp nhận hồ sơ mới | Không | Có | Không | Không |
| Sửa lý lịch hồ sơ | Không | Có | Không | Không |
| Xác nhận hồ sơ/cấp mã | Không | Có | Không | Không |
| Xem đầy đủ giấy tờ định danh | Không mặc định | Có | Che hoặc từ chối | Che hoặc từ chối |
| Hợp đồng và quá trình công tác | Xem nếu thuộc phạm vi | Quản lý | Xem theo phạm vi | Xem theo hồ sơ |
| Trình độ, minh chứng, chứng chỉ | Xem | Quản lý và upload | Xem | Xem hồ sơ của mình |
| Nâng lương | Xem | Quản lý | Từ chối | Từ chối |
| Khen thưởng, kỷ luật | Xem | Quản lý | Từ chối | Từ chối |
| Giờ giảng và NCKH | Xem | Quản lý | Từ chối chỉnh sửa | Từ chối chỉnh sửa |
| Thông tin gia đình/sức khỏe | Theo chính sách và phạm vi | Quản lý | Từ chối | Chỉ hồ sơ chính mình |
| Xuất CSV, Excel, PDF | Không | Có | Có | Không |
| Quản lý tài khoản | Có | Không | Không | Không |
| Nhật ký đăng nhập | Có | Không | Không | Không |

## 3. Các URL cần kiểm thử

Thay `N` bằng một mã hồ sơ có trong database, ví dụ `1`.

| Chức năng | URL |
|---|---|
| Danh sách hồ sơ | `/NhanSu` |
| Chi tiết hồ sơ | `/NhanSu/Details?id=N` |
| Tạo hồ sơ | `/NhanSu/Create` |
| Báo cáo | `/M01/BaoCao` |
| Xuất CSV | `/M01/XuatBaoCaoCsv` |
| Xuất Excel | `/M01/XuatBaoCaoExcel` |
| Xuất PDF | `/M01/XuatBaoCaoPdf` |
| Trình độ và minh chứng | `/TrinhDo/Index?nhanSuId=N` |
| Chứng chỉ | `/ChungChi/Index?nhanSuId=N` |
| Hợp đồng | `/HopDong/Index?nhanSuId=N` |
| Nâng lương | `/M01/NangLuong?nhanSuId=N` |
| Khen thưởng | `/M01/KhenThuong?nhanSuId=N` |
| Kỷ luật | `/M01/KyLuat?nhanSuId=N` |
| Giờ giảng | `/M01/GioGiang?nhanSuId=N` |
| NCKH | `/M01/Nckh?nhanSuId=N` |
| Thông tin nhạy cảm | `/M01/NhayCam?nhanSuId=N` |
| Quá trình công tác | `/QuaTrinhCongTac/Index?nhanSuId=N` |
| Quản lý tài khoản | `/AdminUsers` |
| Nhật ký đăng nhập | `/AdminUsers/LoginLogs` |

## 4. Checklist theo từng vai trò

### 4.1 Admin

- [ ] Đăng nhập thành công và thấy **Tài khoản**, **Nhật ký đăng nhập**.
- [ ] Vào `/NhanSu`, tìm kiếm và xem chi tiết hồ sơ.
- [ ] Vào `/AdminUsers`, tạo thử tài khoản với dữ liệu kiểm thử.
- [ ] Khóa và mở khóa tài khoản kiểm thử.
- [ ] Vào `/AdminUsers/LoginLogs`, lọc `Success`, `Failed`, `LockedOut`.
- [ ] Truy cập `/NhanSu/Create`, `/NhanSu/Edit?id=N`, `/M01/XuatBaoCaoPdf`.
- [ ] Kỳ vọng: thao tác bị từ chối nếu Admin chưa được cấp quyền tương ứng; không tự suy ra quyền từ tên vai trò.

### 4.2 HR

- [ ] Xem danh sách, tìm kiếm và lọc hồ sơ.
- [ ] Tạo mới và sửa một hồ sơ kiểm thử.
- [ ] Sửa giấy tờ định danh và xác nhận hồ sơ.
- [ ] Thêm/sửa hợp đồng, quá trình công tác, trình độ, chứng chỉ.
- [ ] Upload một PDF/JPG/PNG hợp lệ.
- [ ] Thử thông tin gia đình, sức khỏe và lịch sử sức khỏe; kiểm tra validation khi bỏ trống trường bắt buộc.
- [ ] Thêm dữ liệu nâng lương, khen thưởng, kỷ luật, giờ giảng và NCKH.
- [ ] Tải thành công CSV, Excel và PDF từ `/M01/BaoCao`.
- [ ] Kiểm tra audit log sau các thao tác tạo, sửa, upload và xuất báo cáo.

### 4.3 Manager

- [ ] Xem được hồ sơ thuộc đơn vị được gán.
- [ ] Không xem được hồ sơ ngoài đơn vị hoặc không thấy dữ liệu ngoài phạm vi.
- [ ] Tìm kiếm, lọc và phân trang trong phạm vi.
- [ ] Tải được CSV, Excel và PDF.
- [ ] Thử các URL tạo/sửa/xác nhận hồ sơ, nâng lương, khen thưởng, kỷ luật, giờ giảng, NCKH và upload.
- [ ] Kỳ vọng: request ghi dữ liệu bị từ chối; không chỉ dựa vào việc nút bị ẩn.

### 4.4 Lecturer

- [ ] Xem được hồ sơ được liên kết với tài khoản.
- [ ] Thử mở một hồ sơ khác bằng cách đổi `id` hoặc `nhanSuId`.
- [ ] Kỳ vọng: hồ sơ ngoài phạm vi bị từ chối hoặc không được tiết lộ.
- [ ] Xem dữ liệu công khai và dữ liệu cá nhân theo chính sách.
- [ ] Thử tạo/sửa/xác nhận hồ sơ, upload, nâng lương, khen thưởng, kỷ luật, giờ giảng và NCKH.
- [ ] Kỳ vọng: không được thực hiện các thao tác quản trị hoặc ghi dữ liệu.
- [ ] Vào `/M01/BaoCao`; không có quyền xuất file và các endpoint xuất file bị từ chối.

## 5. Kết quả cần ghi nhận

Mỗi ô cần ghi `Đạt` hoặc `Không đạt`, kèm HTTP status và ghi chú:

| Ngày | Vai trò | Chức năng | Kết quả UI | HTTP status | Ghi chú |
|---|---|---|---|---|---|
|  |  |  |  |  |  |

Quy ước kỳ vọng:

- Trang được phép: `200 OK`.
- Tải file được phép: tải đúng phần mở rộng và nội dung.
- Request không có quyền: `403 Forbidden`, `404 Not Found` hoặc chuyển đến trang lỗi theo chính sách ứng dụng.
- Không được làm lộ dữ liệu hồ sơ ngoài phạm vi.
- Thao tác ghi thành công phải có bản ghi `AuditLogs` tương ứng.

## 6. Quy trình chạy nhanh

1. Khởi động ứng dụng bằng profile HTTPS.
2. Đăng nhập một vai trò, thực hiện đúng checklist của vai trò đó.
3. Đăng xuất hoàn toàn.
4. Lặp lại với ba vai trò còn lại.
5. Thử trực tiếp các URL bị hạn chế.
6. Kiểm tra dữ liệu, file upload và `AuditLogs` sau kiểm thử.
7. Không dùng mật khẩu thật trong ảnh chụp hoặc tài liệu nghiệm thu.
