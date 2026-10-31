# M30 - Bộ ca kiểm thử tải tệp an toàn

Thực hiện với tài khoản có quyền `HoSo.Sua`, trên hồ sơ được phép truy cập.

| Mã | Dữ liệu kiểm thử | Kết quả mong đợi |
|---|---|---|
| M30-01 | PDF/JPG/PNG hợp lệ, kích thước không quá 5 MB, magic bytes đúng | Tải lên thành công; tệp lưu ngoài `wwwroot` với tên GUID |
| M30-02 | Tệp `.exe` hoặc đổi tên thành `.exe` | Từ chối với lỗi định dạng; không tạo tệp trên đĩa |
| M30-03 | Đổi tên file thực thi/text thành `.pdf` hoặc `.jpg`, magic bytes không khớp | Từ chối do nội dung không khớp phần mở rộng |
| M30-04 | PDF/JPG/PNG lớn hơn 5 MB hoặc request vượt giới hạn | Từ chối; không ghi tệp dở dang và không tạo bản ghi minh chứng |

## Kiểm tra sau mỗi ca

- Xác nhận không có đường dẫn người dùng kiểm soát được trong tên lưu.
- Xác nhận tải xuống chỉ hoạt động khi người dùng có scope hồ sơ.
- Xác nhận response tải xuống có `X-Content-Type-Options: nosniff`.
- Kiểm tra audit log không ghi nội dung tệp hoặc dữ liệu bí mật.
