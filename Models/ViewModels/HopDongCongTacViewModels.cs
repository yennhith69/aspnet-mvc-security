using System.ComponentModel.DataAnnotations;

public static class HopDongText
{
    public static readonly string[] Loais = { "TapSu", "GiangThu", "XacDinhThoiHan", "KhongXacDinhThoiHan" };

    public static string Loai(string v) => v switch
    {
        "TapSu" => "Tập sự",
        "GiangThu" => "Giảng thử",
        "XacDinhThoiHan" => "Xác định thời hạn",
        "KhongXacDinhThoiHan" => "Không xác định thời hạn",
        _ => v
    };

    public static string TrangThai(HopDong h)
    {
        if (h.TrangThai == "ChamDut") return "Đã chấm dứt";
        if (h.DenNgay.HasValue && h.DenNgay.Value.Date < DateTime.Today) return "Hết hạn";
        return "Hiệu lực";
    }
}

public class HopDongIndexViewModel
{
    public int NhanSuId { get; set; }
    public string MaHrm { get; set; } = "";
    public string HoTen { get; set; } = "";
    public List<HopDong> Items { get; set; } = new();
    public bool CoQuyenSua { get; set; }
}

public class HopDongCreateViewModel
{
    public int NhanSuId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại hợp đồng.")]
    public string Loai { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập ngày bắt đầu.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? TuNgay { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DenNgay { get; set; }
}

public class HopDongSapHetHanRow
{
    public int Id { get; set; }
    public int NhanSuId { get; set; }
    public string MaHrm { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string Loai { get; set; } = "";
    public DateTime DenNgay { get; set; }
}

public class HopDongSapHetHanViewModel
{
    public List<HopDongSapHetHanRow> Items { get; set; } = new();
    public int Days { get; set; } = 60;
}

public class CongTacRow
{
    public int Id { get; set; }
    public string DonVi { get; set; } = "";
    public string? ChucVu { get; set; }
    public DateTime TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
}

public class CongTacIndexViewModel
{
    public int NhanSuId { get; set; }
    public string MaHrm { get; set; } = "";
    public string HoTen { get; set; } = "";
    public List<CongTacRow> Items { get; set; } = new();
    public bool CoQuyenSua { get; set; }
}

public class CongTacCreateViewModel
{
    public int NhanSuId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn đơn vị.")]
    public int? DonViId { get; set; }

    public int? ChucVuId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập ngày bắt đầu.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? TuNgay { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DenNgay { get; set; }
}