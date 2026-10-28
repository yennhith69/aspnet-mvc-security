using System.ComponentModel.DataAnnotations;

public class ApiLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class ApiLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}

public class ApiNhanSuDto
{
    public int Id { get; set; }
    public string MaHrm { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string DonVi { get; set; } = string.Empty;
    public string TinhTrangCongTac { get; set; } = string.Empty;
    public string TrangThaiHoSo { get; set; } = string.Empty;
}