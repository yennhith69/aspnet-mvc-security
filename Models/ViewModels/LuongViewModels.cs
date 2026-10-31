using System.ComponentModel.DataAnnotations;

public class LuongReportRow
{
    public int NhanSuId { get; set; }
    public string MaHrm { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string DonVi { get; set; } = "";
    public string Bac { get; set; } = "";
    public decimal MucLuong { get; set; }
    public DateTime NgayHieuLuc { get; set; }
    public bool IsMasked { get; set; }
    public string MucLuongHienThi => IsMasked ? "******" : MucLuong.ToString("N0");
}

public class LuongReportViewModel
{
    public List<LuongReportRow> Items { get; set; } = new();
    public bool CanManage { get; set; }
    public bool CanViewPlaintext { get; set; }
}

public class LuongCreateViewModel
{
    public int NhanSuId { get; set; }

    [Required, StringLength(50)]
    public string Bac { get; set; } = "";

    [Range(typeof(decimal), "0.01", "999999999999")]
    public decimal MucLuong { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime? NgayHieuLuc { get; set; }
}

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
