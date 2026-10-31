using System.ComponentModel.DataAnnotations;

public class ThongTinNhayCamViewModel
{
    public int NhanSuId { get; set; }
    public string HoTen { get; set; } = "";
    public string? GiaDinh { get; set; }
    public string? SucKhoe { get; set; }
}

public class NangLuongViewModel
{
    public int NhanSuId { get; set; }
    public int? Id { get; set; }
    public string HoTen { get; set; } = "";
    public List<NangLuong> Items { get; set; } = new();
    [Required, StringLength(50)] public string Bac { get; set; } = "";
    [Range(typeof(decimal), "0.01", "999999999999", ParseLimitsInInvariantCulture = true)]
    public decimal MucLuong { get; set; }
    [Required, DataType(DataType.Date)] public DateTime? NgayHieuLuc { get; set; }
}

public class KhenThuongViewModel
{
    public int NhanSuId { get; set; }
    public int? Id { get; set; }
    public string HoTen { get; set; } = "";
    public List<KhenThuong> Items { get; set; } = new();
    [Required, StringLength(100)] public string Loai { get; set; } = "";
    [Required, StringLength(500)] public string NoiDung { get; set; } = "";
    [Required, DataType(DataType.Date)] public DateTime? Ngay { get; set; }
}

public class KyLuatViewModel
{
    public int NhanSuId { get; set; }
    public int? Id { get; set; }
    public string HoTen { get; set; } = "";
    public List<KyLuat> Items { get; set; } = new();
    [Required, StringLength(100)] public string Loai { get; set; } = "";
    [Required, StringLength(500)] public string NoiDung { get; set; } = "";
    [Required, DataType(DataType.Date)] public DateTime? Ngay { get; set; }
}

public class GioGiangViewModel
{
    public int NhanSuId { get; set; }
    public int? Id { get; set; }
    public string HoTen { get; set; } = "";
    public List<GioGiang> Items { get; set; } = new();
    [Required, StringLength(20)] public string NamHoc { get; set; } = "";
    [Range(typeof(decimal), "0", "10000")] public decimal SoGio { get; set; }
}

public class NckhViewModel
{
    public int NhanSuId { get; set; }
    public int? Id { get; set; }
    public string HoTen { get; set; } = "";
    public List<HoatDongNCKH> Items { get; set; } = new();
    [Required, StringLength(300)] public string TenHoatDong { get; set; } = "";
    [Range(2000, 2100)] public int Nam { get; set; } = DateTime.Today.Year;
}

public class M01ReportViewModel
{
    public int TongNhanSu { get; set; }
    public int HoSoDaXacNhan { get; set; }
    public int HopDongSapHetHan { get; set; }
    public int HoatDongNckh { get; set; }
    public decimal TongGioGiang { get; set; }
}
