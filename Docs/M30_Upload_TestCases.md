# M30 - Bộ ca kiểm thử upload

## Phạm vi

Thực hiện với tài khoản có quyền sửa hồ sơ và một hồ sơ trong phạm vi được cấp. Thư mục lưu tệp lấy theo `FileStorage:UploadPath`, mặc định là `App_Data/minhchung` và nằm ngoài `wwwroot`.

| Ca | Dữ liệu | Kỳ vọng |
|---|---|---|
| M30-01 | PDF/JPG/PNG hợp lệ, <= 5 MB | Thành công, tên GUID, lưu ngoài `wwwroot` | Chưa chạy trong môi trường nghiệm thu |
| M30-02 | `.exe` hoặc phần mở rộng không whitelist | Bị từ chối, không tạo file/bản ghi | Chưa chạy trong môi trường nghiệm thu |
| M30-03 | Đổi file text/exe thành `.pdf` hoặc `.jpg` | Bị từ chối do magic bytes | Chưa chạy trong môi trường nghiệm thu |
| M30-04 | File > 5 MB | Bị từ chối bởi validation/request limit | Chưa chạy trong môi trường nghiệm thu |

## Cách thực hiện và bằng chứng

1. Ghi số lượng bản ghi `TepMinhChungs` và danh sách tệp trong thư mục lưu trước khi chạy.
2. Thực hiện từng ca tại `Trình độ và minh chứng`, ghi HTTP status/thông báo.
3. Với ca hợp lệ, kiểm tra tên lưu là GUID, phần mở rộng whitelist và đường dẫn không nằm trong `wwwroot`.
4. Với ca bị từ chối, xác nhận số bản ghi và số tệp không tăng.
5. Kiểm tra `AuditLogs` theo hành động upload/từ chối và lưu ảnh màn hình kết quả.

Trạng thái hiện tại: logic whitelist, giới hạn kích thước, magic bytes, tên GUID và audit đã được build xác minh; bốn ca trên cần chạy thủ công khi ứng dụng kết nối được database nghiệm thu.
