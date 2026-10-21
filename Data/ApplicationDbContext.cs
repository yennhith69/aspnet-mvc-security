using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<DonVi> DonVis => Set<DonVi>();
    public DbSet<ChucVu> ChucVus => Set<ChucVu>();
    public DbSet<NhanSu> NhanSus => Set<NhanSu>();
    public DbSet<GiayToDinhDanh> GiayToDinhDanhs => Set<GiayToDinhDanh>();
    public DbSet<QuaTrinhCongTac> QuaTrinhCongTacs => Set<QuaTrinhCongTac>();
    public DbSet<HopDong> HopDongs => Set<HopDong>();
    public DbSet<TepMinhChung> TepMinhChungs => Set<TepMinhChung>();
    public DbSet<TrinhDo> TrinhDos => Set<TrinhDo>();
    public DbSet<ThongTinNhayCam> ThongTinNhayCams => Set<ThongTinNhayCam>();
    public DbSet<NangLuong> NangLuongs => Set<NangLuong>();
    public DbSet<KhenThuong> KhenThuongs => Set<KhenThuong>();
    public DbSet<GioGiang> GioGiangs => Set<GioGiang>();
    public DbSet<HoatDongNCKH> HoatDongNCKHs => Set<HoatDongNCKH>();
    public DbSet<Quyen> Quyens => Set<Quyen>();
    public DbSet<VaiTroQuyen> VaiTroQuyens => Set<VaiTroQuyen>();
    public DbSet<LichSuMatKhau> LichSuMatKhaus => Set<LichSuMatKhau>();
    public DbSet<LoginLog> LoginLogs => Set<LoginLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Ràng buộc duy nhất
        b.Entity<DonVi>().HasIndex(x => x.Ma).IsUnique();
        b.Entity<Quyen>().HasIndex(x => x.Ma).IsUnique();
        b.Entity<GiayToDinhDanh>().HasIndex(x => new { x.Loai, x.So }).IsUnique();   // kiểm tra trùng giấy tờ

        b.Entity<NhanSu>(e =>
        {
            e.HasIndex(x => x.MaHrm).IsUnique();
            e.HasIndex(x => x.MaDinhDanhNganh).IsUnique().HasFilter("[MaDinhDanhNganh] IS NOT NULL");
            e.HasIndex(x => new { x.DonViId, x.TinhTrangCongTac });   // phục vụ mục 27
            e.HasIndex(x => x.HoTen);                                  // phục vụ mục 27
            e.Property(x => x.MaHrm).HasMaxLength(20);
            e.Property(x => x.HoTen).HasMaxLength(200);
        });

        b.Entity<NangLuong>().Property(x => x.MucLuong).HasPrecision(18, 2);
        b.Entity<GioGiang>().Property(x => x.SoGio).HasPrecision(8, 2);
        b.Entity<LoginLog>().HasIndex(x => x.ThoiGian);
        b.Entity<AuditLog>().HasIndex(x => x.ThoiGian);

        // Khóa ngoại: dùng Restrict để không xóa dây chuyền, đúng quy tắc "không xóa hồ sơ đã liên kết"
        b.Entity<NhanSu>().HasOne(x => x.DonVi).WithMany().HasForeignKey(x => x.DonViId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<NhanSu>().HasOne<ChucVu>().WithMany().HasForeignKey(x => x.ChucVuId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<GiayToDinhDanh>().HasOne<NhanSu>().WithMany().HasForeignKey(x => x.NhanSuId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<QuaTrinhCongTac>().HasOne<NhanSu>().WithMany().HasForeignKey(x => x.NhanSuId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<QuaTrinhCongTac>().HasOne<DonVi>().WithMany().HasForeignKey(x => x.DonViId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<QuaTrinhCongTac>().HasOne<ChucVu>().WithMany().HasForeignKey(x => x.ChucVuId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HopDong>().HasOne<NhanSu>().WithMany().HasForeignKey(x => x.NhanSuId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TepMinhChung>().HasOne<NhanSu>().WithMany().HasForeignKey(x => x.NhanSuId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TrinhDo>().HasOne<NhanSu>().WithMany().HasForeignKey(x => x.NhanSuId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TrinhDo>().HasOne<TepMinhChung>().WithMany().HasForeignKey(x => x.TepMinhChungId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ThongTinNhayCam>().HasOne<NhanSu>().WithMany().HasForeignKey(x => x.NhanSuId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<NangLuong>().HasOne<NhanSu>().WithMany().HasForeignKey(x => x.NhanSuId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<KhenThuong>().HasOne<NhanSu>().WithMany().HasForeignKey(x => x.NhanSuId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GioGiang>().HasOne<NhanSu>().WithMany().HasForeignKey(x => x.NhanSuId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HoatDongNCKH>().HasOne<NhanSu>().WithMany().HasForeignKey(x => x.NhanSuId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<VaiTroQuyen>().HasOne(x => x.Quyen).WithMany().HasForeignKey(x => x.QuyenId).OnDelete(DeleteBehavior.Restrict);
    }
}