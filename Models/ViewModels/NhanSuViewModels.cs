using System.ComponentModel.DataAnnotations;

public static class NhanSuText
{
    public static readonly string[] GioiTinhs = { "Nam", "Nữ", "Khác" };
    public static readonly string[] LoaiGiayTos = { "CCCD", "CMND", "HoChieu" };
    public static readonly string[] TinhTrangs = { "DangCongTac", "TapSu", "GiangThu", "NghiViec" };
    public static readonly string[] TrangThais = { "TiepNhan", "DaKiemTraTrung", "DaCapMa", "DaXacNhan" };

    public static string TinhTrang(string v) => v switch
    {
        "DangCongTac" => "Đang công tác",
        "TapSu" => "Tập sự",
        "GiangThu" => "Giảng thử",
        "NghiViec" => "Nghỉ việc",
        _ => v
    };

    public static string TrangThai(string v) => v switch
    {
        "TiepNhan" => "Tiếp nhận",
        "DaKiemTraTrung" => "Đã kiểm tra trùng",
        "DaCapMa" => "Đã cấp mã",
        "DaXacNhan" => "Đã xác nhận",
        _ => v
    };
}

public class NhanSuListItem
{
    public int Id { get; set; }
    public string MaHrm { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string DonVi { get; set; } = "";
    public string TinhTrangCongTac { get; set; } = "";
    public string TrangThaiHoSo { get; set; } = "";
    public bool CongKhai { get; set; }
}

public class NhanSuIndexViewModel
{
    public List<NhanSuListItem> Items { get; set; } = new();
    public List<DonVi> DonVis { get; set; } = new();
    public string? Q { get; set; }
    public int? DonViId { get; set; }
    public string? TinhTrang { get; set; }
    public string? TrangThai { get; set; }
    public string Sort { get; set; } = "ma";
    public string Dir { get; set; } = "asc";
    public int PageNumber { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int Total { get; set; }
    public bool CoQuyenSua { get; set; }
}

public class NhanSuCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(200)]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập ngày sinh.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? NgaySinh { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
    public string GioiTinh { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256)]
    public string? Email { get; set; }

    [RegularExpression(@"^[0-9+\s\-]{8,15}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? DienThoai { get; set; }

    [StringLength(500)]
    public string? DiaChiCuTru { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn đơn vị.")]
    public int? DonViId { get; set; }

    public int? ChucVuId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại giấy tờ.")]
    public string LoaiGiayTo { get; set; } = "CCCD";

    [Required(ErrorMessage = "Vui lòng nhập số giấy tờ.")]
    [RegularExpression(@"^[A-Za-z0-9]{6,20}$", ErrorMessage = "Số giấy tờ gồm 6 đến 20 chữ hoặc số.")]
    public string SoGiayTo { get; set; } = "";

    public bool XacNhanKhongTrung { get; set; }
}

public class NhanSuEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(200)]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập ngày sinh.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? NgaySinh { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
    public string GioiTinh { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256)]
    public string? Email { get; set; }

    [RegularExpression(@"^[0-9+\s\-]{8,15}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string? DienThoai { get; set; }

    [StringLength(500)]
    public string? DiaChiCuTru { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn đơn vị.")]
    public int? DonViId { get; set; }

    public int? ChucVuId { get; set; }

    [Required]
    public string TinhTrangCongTac { get; set; } = "DangCongTac";

    public bool CongKhai { get; set; }
}

public class GiayToView
{
    public int Id { get; set; }
    public string Loai { get; set; } = "";
    public string So { get; set; } = "";
    public DateTime? NgayCap { get; set; }
    public string? NoiCap { get; set; }
}

public class GiayToEditViewModel
{
    public int Id { get; set; }
    public int NhanSuId { get; set; }
    public string HoTen { get; set; } = "";
    [Required] public string Loai { get; set; } = "";
    [Required, StringLength(20)] public string So { get; set; } = "";
    [DataType(DataType.Date)] public DateTime? NgayCap { get; set; }
    [StringLength(200)] public string? NoiCap { get; set; }
}

public class NhanSuDetailsViewModel
{
    public NhanSu NhanSu { get; set; } = new();
    public string DonViTen { get; set; } = "";
    public string? ChucVuTen { get; set; }
    public List<GiayToView> GiayTos { get; set; } = new();
    public bool CoQuyenSua { get; set; }
    public bool CoQuyenXacNhan { get; set; }
}