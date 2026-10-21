using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class Roles
{
    public const string Admin = "Administrator", Hr = "HR", Manager = "Manager", Lecturer = "Lecturer";
    public static readonly string[] All = { Admin, Hr, Manager, Lecturer };
}

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, IConfiguration cfg)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        // 1. Vai trò
        var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var r in Roles.All)
            if (!await roleMgr.RoleExistsAsync(r)) await roleMgr.CreateAsync(new IdentityRole(r));

        // 2. Đơn vị và chức vụ
        if (!await db.DonVis.AnyAsync())
        {
            db.DonVis.AddRange(new DonVi { Ma = "CNTT", Ten = "Khoa Công nghệ thông tin" },
                               new DonVi { Ma = "KT", Ten = "Khoa Kinh tế" });
            db.ChucVus.AddRange(new ChucVu { Ten = "Giảng viên" }, new ChucVu { Ten = "Trưởng khoa" });
            await db.SaveChangesAsync();
        }

        // 3. Quyền và gán quyền cho vai trò (mục 16)
        if (!await db.Quyens.AnyAsync())
        {
            db.Quyens.AddRange(
                new Quyen { Ma = "HoSo.Xem", MoTa = "Xem hồ sơ" },
                new Quyen { Ma = "HoSo.Sua", MoTa = "Thêm, sửa hồ sơ" },
                new Quyen { Ma = "HoSo.XacNhan", MoTa = "Xác nhận hồ sơ, cấp mã" },
                new Quyen { Ma = "GiayTo.Xem", MoTa = "Xem số giấy tờ đầy đủ" },
                new Quyen { Ma = "Luong.Xem", MoTa = "Xem mức lương" },
                new Quyen { Ma = "BaoCao.In", MoTa = "Xem và in báo cáo" },
                new Quyen { Ma = "Admin.QuanTri", MoTa = "Quản lý tài khoản, xem nhật ký" });
            await db.SaveChangesAsync();

            var q = await db.Quyens.ToDictionaryAsync(x => x.Ma, x => x.Id);
            void Grant(string role, params string[] codes)
            {
                foreach (var c in codes)
                    db.VaiTroQuyens.Add(new VaiTroQuyen { RoleName = role, QuyenId = q[c] });
            }

            Grant(Roles.Admin, "Admin.QuanTri", "HoSo.Xem");
            Grant(Roles.Hr, "HoSo.Xem", "HoSo.Sua", "HoSo.XacNhan", "GiayTo.Xem", "Luong.Xem", "BaoCao.In");
            Grant(Roles.Manager, "HoSo.Xem", "BaoCao.In");
            Grant(Roles.Lecturer, "HoSo.Xem");
            await db.SaveChangesAsync();
        }

        // 4. Hồ sơ mẫu (dữ liệu giả)
        if (!await db.NhanSus.AnyAsync())
        {
            var cntt = await db.DonVis.FirstAsync(x => x.Ma == "CNTT");
            var kt = await db.DonVis.FirstAsync(x => x.Ma == "KT");
            var gv = await db.ChucVus.FirstAsync(x => x.Ten == "Giảng viên");

            var list = new[]
            {
                new NhanSu { MaHrm = "GV2026-0001", HoTen = "Nguyễn Văn An", NgaySinh = new DateTime(1985, 3, 12), GioiTinh = "Nam",
                    Email = "an.nv@example.test", DienThoai = "0900000001", DonViId = cntt.Id, ChucVuId = gv.Id,
                    TrangThaiHoSo = "DaXacNhan", CongKhai = true },
                new NhanSu { MaHrm = "GV2026-0002", HoTen = "Trần Thị Bình", NgaySinh = new DateTime(1990, 7, 21), GioiTinh = "Nữ",
                    Email = "binh.tt@example.test", DienThoai = "0900000002", DonViId = cntt.Id, ChucVuId = gv.Id,
                    TrangThaiHoSo = "DaXacNhan", CongKhai = false },
                new NhanSu { MaHrm = "GV2026-0003", HoTen = "Lê Minh Châu", NgaySinh = new DateTime(1988, 11, 5), GioiTinh = "Nữ",
                    Email = "chau.lm@example.test", DienThoai = "0900000003", DonViId = kt.Id, ChucVuId = gv.Id,
                    TrangThaiHoSo = "DaCapMa", CongKhai = true }
            };
            db.NhanSus.AddRange(list);
            await db.SaveChangesAsync();

            for (int i = 0; i < list.Length; i++)
            {
                db.GiayToDinhDanhs.Add(new GiayToDinhDanh { NhanSuId = list[i].Id, Loai = "CCCD", So = $"00000000000{i + 1}" });
                db.NangLuongs.Add(new NangLuong { NhanSuId = list[i].Id, Bac = "Bậc 3", MucLuong = 12_000_000 + i * 500_000, NgayHieuLuc = new DateTime(2025, 1, 1) });
            }
            await db.SaveChangesAsync();
        }

        // 5. Tài khoản mẫu: mật khẩu lấy từ User Secrets, không viết trong mã
        var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var cnttId = (await db.DonVis.FirstAsync(x => x.Ma == "CNTT")).Id;
        var gv1Id = (await db.NhanSus.FirstAsync(x => x.MaHrm == "GV2026-0001")).Id;

        async Task Create(string email, string fullName, string role, string secretKey, int? donViId = null, int? nhanSuId = null)
        {
            var pwd = cfg[secretKey];
            if (string.IsNullOrEmpty(pwd) || await userMgr.FindByEmailAsync(email) is not null) return;
            var u = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                DonViId = donViId,
                NhanSuId = nhanSuId
            };
            if ((await userMgr.CreateAsync(u, pwd)).Succeeded) await userMgr.AddToRoleAsync(u, role);
        }

        await Create("admin@hrm.test", "Quản trị hệ thống", Roles.Admin, "Seed:AdminPassword");
        await Create("hr@hrm.test", "Cán bộ nhân sự", Roles.Hr, "Seed:HrPassword");
        await Create("manager@hrm.test", "Trưởng khoa CNTT", Roles.Manager, "Seed:ManagerPassword", donViId: cnttId);
        await Create("lecturer@hrm.test", "Nguyễn Văn An", Roles.Lecturer, "Seed:LecturerPassword", nhanSuId: gv1Id);
    }
}