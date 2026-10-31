# M30 - Bộ ca kiểm thử upload

| Ca | Dữ liệu | Kỳ vọng |
|---|---|---|
| M30-01 | PDF/JPG/PNG hợp lệ, <= 5 MB | Thành công, tên GUID, lưu ngoài `wwwroot` |
| M30-02 | `.exe` hoặc phần mở rộng không whitelist | Bị từ chối, không tạo file/bản ghi |
| M30-03 | Đổi file text/exe thành `.pdf` hoặc `.jpg` | Bị từ chối do magic bytes |
| M30-04 | File > 5 MB | Bị từ chối bởi validation/request limit |

Ghi lại HTTP status, thông báo, bản ghi AuditLog và trạng thái thư mục lưu sau mỗi ca.
