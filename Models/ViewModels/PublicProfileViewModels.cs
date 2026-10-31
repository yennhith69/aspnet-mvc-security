public class PublicProfileViewModel
{
    public string MaHrm { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string DonVi { get; set; } = "";
    public string? ChucVu { get; set; }
    public List<PublicEducationRow> TrinhDos { get; set; } = new();
    public List<PublicResearchRow> HoatDongNckh { get; set; } = new();
}

public class PublicResearchRow
{
    public string TenHoatDong { get; set; } = "";
    public int Nam { get; set; }
}

public class PublicProfileListItem
{
    public int Id { get; set; }
    public string MaHrm { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string DonVi { get; set; } = "";
    public string? ChucVu { get; set; }
}

public class PublicProfileDirectoryViewModel
{
    public string? Query { get; set; }
    public List<PublicProfileListItem> Items { get; set; } = new();
}

public class PublicEducationRow
{
    public string Loai { get; set; } = "";
    public string ChuyenNganh { get; set; } = "";
    public string DonViCap { get; set; } = "";
}
