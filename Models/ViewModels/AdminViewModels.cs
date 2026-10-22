using System.ComponentModel.DataAnnotations;

public class UserRowViewModel
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Roles { get; set; } = "";
    public bool IsLocked { get; set; }
    public int AccessFailedCount { get; set; }
}

public class CreateUserViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(200)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    public string Role { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu tạm thời.")]
    [DataType(DataType.Password)]
    [StringLength(128, MinimumLength = 10, ErrorMessage = "Mật khẩu phải từ 10 đến 128 ký tự.")]
    public string Password { get; set; } = "";

    public int? DonViId { get; set; }     // dùng cho vai trò Manager
    public int? NhanSuId { get; set; }    // dùng cho vai trò Lecturer
}