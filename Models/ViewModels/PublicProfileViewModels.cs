public class PublicProfileViewModel
{
    public string MaHrm { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string DonVi { get; set; } = "";
    public string? ChucVu { get; set; }
    public List<PublicEducationRow> TrinhDos { get; set; } = new();
}

public class PublicEducationRow
{
    public string Loai { get; set; } = "";
    public string ChuyenNganh { get; set; } = "";
    public string DonViCap { get; set; } = "";
}
