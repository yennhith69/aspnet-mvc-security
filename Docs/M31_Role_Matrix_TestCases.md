# M31 - Ma trận kiểm thử phân quyền

| Chức năng | Admin | HR | Manager | Lecturer |
|---|---|---|---|---|
| Xem hồ sơ trong phạm vi | Cho phép | Cho phép | Cho phép theo đơn vị | Cho phép theo hồ sơ |
| Sửa hồ sơ | Theo quyền DB | Cho phép | Từ chối nếu không được cấp | Từ chối |
| Xác nhận hồ sơ | Theo quyền DB | Theo quyền DB | Từ chối | Từ chối |
| Xem giấy tờ định danh | Theo quyền DB | Cho phép | Che/mask nếu thiếu quyền | Che/mask |
| Nâng lương | Theo quyền DB | Cho phép | Từ chối | Từ chối |
| Báo cáo/in/xuất | Theo quyền DB | Cho phép | Cho phép nếu có quyền | Từ chối nếu thiếu quyền |
| Thông tin gia đình/sức khỏe | Admin/HR hoặc chính chủ | HR hoặc chính chủ | Từ chối | Chính chủ |
| Upload minh chứng | Theo quyền DB | Cho phép | Từ chối | Từ chối |

## Cách chạy

Đăng nhập lần lượt bằng từng tài khoản, thử cả URL trực tiếp và nút giao diện. Ghi lại HTTP status, kết quả hiển thị, phạm vi hồ sơ và log audit. Không coi việc ẩn nút giao diện là đủ; phải kiểm tra request trực tiếp.

Trạng thái hiện tại: ma trận được đối chiếu với policy/RBAC và scope trong mã nguồn; cần chạy nghiệm thu với tài khoản thật để ghi bằng chứng runtime.
