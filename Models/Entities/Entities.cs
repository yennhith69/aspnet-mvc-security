using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public int? DonViId { get; set; }     // phạm vi đơn vị của Manager (mục 3)
    public int? NhanSuId { get; set; }    // hồ sơ của chính giảng viên (mục 18)
}

public class DonVi { public int Id { get; set; } public string Ma { get; set; } = ""; public string Ten { get; set; } = ""; }
public class ChucVu { public int Id { get; set; } public string Ten { get; set; } = ""; }

public class NhanSu
{
    public int Id { get; set; }
    public string MaHrm { get; set; } = "";               // hệ thống cấp, unique
    public string? MaDinhDanhNganh { get; set; }          // khác MaHrm, khác số giấy tờ
    public string HoTen { get; set; } = "";
    public DateTime NgaySinh { get; set; }
    public string GioiTinh { get; set; } = "";
    public string? Email { get; set; }
    public string? DienThoai { get; set; }
    public string? DiaChiCuTru { get; set; }
    public int DonViId { get; set; }
    public DonVi? DonVi { get; set; }
    public int? ChucVuId { get; set; }
    public string TinhTrangCongTac { get; set; } = "DangCongTac";  // DangCongTac, TapSu, GiangThu, NghiViec
    public string TrangThaiHoSo { get; set; } = "TiepNhan";        // TiepNhan, DaKiemTraTrung, DaCapMa, DaXacNhan
    public bool CongKhai { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}

public class GiayToDinhDanh
{
    public int Id { get; set; }
    public int NhanSuId { get; set; }
    public string Loai { get; set; } = "CCCD";
    public string So { get; set; } = ""; public string? NoiCap { get; set; }
    public DateTime? NgayCap { get; set; }
}

public class QuaTrinhCongTac
{
    public int Id { get; set; }
    public int NhanSuId { get; set; }
    public int DonViId { get; set; }
    public int? ChucVuId { get; set; }
    public DateTime TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
}

public class HopDong
{
    public int Id { get; set; }
    public int NhanSuId { get; set; }
    public string Loai { get; set; } = "";
    public DateTime TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public string TrangThai { get; set; } = "HieuLuc";
}

public class TepMinhChung
{
    public int Id { get; set; }
    public int NhanSuId { get; set; }
    public string TenGoc { get; set; } = "";
    public string TenLuu { get; set; } = ""; public long KichThuoc { get; set; }
    public string? MoTa { get; set; }
}

public class TrinhDo
{
    public int Id { get; set; }
    public int NhanSuId { get; set; }
    public string Loai { get; set; } = "";
    public string ChuyenNganh { get; set; } = ""; public string DonViCap { get; set; } = ""; public int? TepMinhChungId { get; set; }
}

public class ThongTinNhayCam   // gia đình, sức khỏe: chỉ HR và chính chủ
{ public int Id { get; set; } public int NhanSuId { get; set; } public string? GiaDinh { get; set; } public string? SucKhoe { get; set; } }

public class NangLuong
{
    public int Id { get; set; }
    public int NhanSuId { get; set; }
    public string Bac { get; set; } = "";
    public decimal MucLuong { get; set; }
    public DateTime NgayHieuLuc { get; set; }
}

public class KhenThuong
{
    public int Id { get; set; }
    public int NhanSuId { get; set; }
    public string Loai { get; set; } = "";
    public string NoiDung { get; set; } = ""; public DateTime Ngay { get; set; }
}

public class KyLuat
{
    public int Id { get; set; }
    public int NhanSuId { get; set; }
    public string Loai { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public DateTime Ngay { get; set; }
}

public class GioGiang { public int Id { get; set; } public int NhanSuId { get; set; } public string NamHoc { get; set; } = ""; public decimal SoGio { get; set; } }
public class HoatDongNCKH { public int Id { get; set; } public int NhanSuId { get; set; } public string TenHoatDong { get; set; } = ""; public int Nam { get; set; } }

// Mục 16: quyền đọc từ database
public class Quyen { public int Id { get; set; } public string Ma { get; set; } = ""; public string MoTa { get; set; } = ""; }
public class VaiTroQuyen { public int Id { get; set; } public string RoleName { get; set; } = ""; public int QuyenId { get; set; } public Quyen? Quyen { get; set; } }

// Mục 17, 15, 20
public class LichSuMatKhau { public int Id { get; set; } public string UserId { get; set; } = ""; public string PasswordHash { get; set; } = ""; public DateTime NgayDoi { get; set; } = DateTime.UtcNow; }
public class LoginLog
{
    public long Id { get; set; }
    public string UserName { get; set; } = ""; public string? UserId { get; set; }
    public string KetQua { get; set; } = ""; public string? Ip { get; set; }
    public DateTime ThoiGian { get; set; } = DateTime.UtcNow;
}
public class AuditLog
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string UserName { get; set; } = "";
    public string HanhDong { get; set; } = ""; public string DoiTuong { get; set; } = ""; public string? DoiTuongId { get; set; }
    public string? Ip { get; set; }
    public string? ChiTiet { get; set; }
    public DateTime ThoiGian { get; set; } = DateTime.UtcNow;
}