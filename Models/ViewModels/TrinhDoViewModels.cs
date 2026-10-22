using System.ComponentModel.DataAnnotations;

public static class TrinhDoText
{
    public static readonly string[] Loais = { "CuNhan", "ThacSi", "TienSi", "PhoGiaoSu", "GiaoSu" };

    public static string Loai(string v) => v switch
    {
        "CuNhan" => "Cử nhân",
        "ThacSi" => "Thạc sĩ",
        "TienSi" => "Tiến sĩ",
        "PhoGiaoSu" => "Phó giáo sư",
        "GiaoSu" => "Giáo sư",
        _ => v
    };
}

public class TrinhDoRow
{
    public int Id { get; set; }
    public string Loai { get; set; } = "";
    public string ChuyenNganh { get; set; } = "";
    public string DonViCap { get; set; } = "";
    public int? TepId { get; set; }
    public string? TenGoc { get; set; }
    public string? MoTa { get; set; }
}

public class TrinhDoIndexViewModel
{
    public int NhanSuId { get; set; }
    public string MaHrm { get; set; } = "";
    public string HoTen { get; set; } = "";
    public List<TrinhDoRow> Items { get; set; } = new();
    public bool CoQuyenSua { get; set; }
}

public class TrinhDoCreateViewModel
{
    public int NhanSuId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại trình độ.")]
    public string Loai { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập chuyên ngành.")]
    [StringLength(200)]
    public string ChuyenNganh { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập đơn vị cấp.")]
    [StringLength(200)]
    public string DonViCap { get; set; } = "";

    [StringLength(500)]
    public string? MoTa { get; set; }

    public IFormFile? Tep { get; set; }
}