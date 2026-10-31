using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

[Authorize(Policy = Permissions.HoSoXem)]
public class M01Controller : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly HoSoScopeService _scope;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAuthorizationService _authorization;

    public M01Controller(ApplicationDbContext db, HoSoScopeService scope,
        UserManager<ApplicationUser> users, IAuthorizationService authorization)
    {
        _db = db;
        _scope = scope;
        _users = users;
        _authorization = authorization;
    }

    [HttpGet]
    public async Task<IActionResult> BaoCao()
    {
        var scoped = await _scope.ScopedAsync(User);
        var ids = scoped.Select(x => x.Id);
        var today = DateTime.Today;
        return View(new M01ReportViewModel
        {
            TongNhanSu = await scoped.CountAsync(),
            HoSoDaXacNhan = await scoped.CountAsync(x => x.TrangThaiHoSo == "DaXacNhan"),
            HopDongSapHetHan = await _db.HopDongs.CountAsync(x => ids.Contains(x.NhanSuId) &&
                x.TrangThai == "HieuLuc" && x.DenNgay >= today && x.DenNgay <= today.AddDays(60)),
            HoatDongNckh = await _db.HoatDongNCKHs.CountAsync(x => ids.Contains(x.NhanSuId)),
            TongGioGiang = await _db.GioGiangs.Where(x => ids.Contains(x.NhanSuId))
                .Select(x => (decimal?)x.SoGio).SumAsync() ?? 0
        });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.BaoCaoIn)]
    public async Task<IActionResult> XuatBaoCaoCsv()
    {
        var scoped = await _scope.ScopedAsync(User);
        var ids = scoped.Select(x => x.Id);
        var today = DateTime.Today;
        var rows = new[]
        {
            new[] { "Chi tieu", "Gia tri" },
            new[] { "Tong nhan su", (await scoped.CountAsync()).ToString() },
            new[] { "Ho so da xac nhan", (await scoped.CountAsync(x => x.TrangThaiHoSo == "DaXacNhan")).ToString() },
            new[] { "Hop dong sap het han trong 60 ngay", (await _db.HopDongs.CountAsync(x =>
                ids.Contains(x.NhanSuId) && x.TrangThai == "HieuLuc" && x.DenNgay >= today &&
                x.DenNgay <= today.AddDays(60))).ToString() },
            new[] { "Hoat dong NCKH", (await _db.HoatDongNCKHs.CountAsync(x => ids.Contains(x.NhanSuId))).ToString() },
            new[] { "Tong gio giang", (await _db.GioGiangs.Where(x => ids.Contains(x.NhanSuId))
                .Select(x => (decimal?)x.SoGio).SumAsync() ?? 0).ToString("0.##") }
        };

        var csv = new StringBuilder();
        foreach (var row in rows)
            csv.AppendLine(string.Join(",", row.Select(CsvEscape)));

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv", $"bao-cao-nhan-su-{DateTime.Today:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> NhayCam(int nhanSuId)
    {
        if (!await CanViewSensitiveAsync(nhanSuId)) return NotFound();
        var ns = await _db.NhanSus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == nhanSuId);
        if (ns == null) return NotFound();
        var item = await _db.ThongTinNhayCams.AsNoTracking().FirstOrDefaultAsync(x => x.NhanSuId == nhanSuId);
        return View(new ThongTinNhayCamViewModel
        {
            NhanSuId = nhanSuId, HoTen = ns.HoTen, GiaDinh = item?.GiaDinh, SucKhoe = item?.SucKhoe
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> NhayCam(ThongTinNhayCamViewModel vm)
    {
        if (!await CanViewSensitiveAsync(vm.NhanSuId)) return NotFound();
        if (!ModelState.IsValid) return View(vm);
        var item = await _db.ThongTinNhayCams.FirstOrDefaultAsync(x => x.NhanSuId == vm.NhanSuId);
        if (item == null)
        {
            item = new ThongTinNhayCam { NhanSuId = vm.NhanSuId };
            _db.ThongTinNhayCams.Add(item);
        }
        item.GiaDinh = vm.GiaDinh?.Trim();
        item.SucKhoe = vm.SucKhoe?.Trim();
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(NhayCam), new { nhanSuId = vm.NhanSuId });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.HoSoXem)]
    public async Task<IActionResult> NangLuong(int nhanSuId)
    {
        if (!User.IsInRole(Roles.Admin) &&
            !(await _authorization.AuthorizeAsync(User, Permissions.LuongXem)).Succeeded)
            return Forbid();
        if (!await _scope.CanViewAsync(User, nhanSuId)) return NotFound();
        return View(await BuildLuongVmAsync(nhanSuId));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> NangLuong(NangLuongViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();
        if (!ModelState.IsValid) return View(vm);
        _db.NangLuongs.Add(new NangLuong
        {
            NhanSuId = vm.NhanSuId, Bac = vm.Bac.Trim(), MucLuong = vm.MucLuong,
            NgayHieuLuc = vm.NgayHieuLuc!.Value
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(NangLuong), new { nhanSuId = vm.NhanSuId });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaNangLuong(NangLuongViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();
        if (!ModelState.IsValid || !vm.Id.HasValue) return RedirectToAction(nameof(NangLuong), new { nhanSuId = vm.NhanSuId });
        var item = await _db.NangLuongs.FirstOrDefaultAsync(x => x.Id == vm.Id && x.NhanSuId == vm.NhanSuId);
        if (item == null) return NotFound();
        item.Bac = vm.Bac.Trim();
        item.MucLuong = vm.MucLuong;
        item.NgayHieuLuc = vm.NgayHieuLuc!.Value;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(NangLuong), new { nhanSuId = vm.NhanSuId });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaNangLuong(int id)
    {
        var item = await _db.NangLuongs.FindAsync(id);
        if (item == null || !await _scope.CanViewAsync(User, item.NhanSuId)) return NotFound();
        _db.NangLuongs.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(NangLuong), new { nhanSuId = item.NhanSuId });
    }

    [HttpGet]
    public async Task<IActionResult> KhenThuong(int nhanSuId)
    {
        if (!await _scope.CanViewAsync(User, nhanSuId)) return NotFound();
        var ns = await _db.NhanSus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == nhanSuId);
        if (ns == null) return NotFound();
        return View(new KhenThuongViewModel
        {
            NhanSuId = nhanSuId, HoTen = ns.HoTen,
            Items = await _db.KhenThuongs.AsNoTracking().Where(x => x.NhanSuId == nhanSuId)
                .OrderByDescending(x => x.Ngay).ToListAsync()
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> KhenThuong(KhenThuongViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();
        if (!ModelState.IsValid) return View(vm);
        _db.KhenThuongs.Add(new KhenThuong { NhanSuId = vm.NhanSuId, Loai = vm.Loai.Trim(), NoiDung = vm.NoiDung.Trim(), Ngay = vm.Ngay!.Value });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(KhenThuong), new { nhanSuId = vm.NhanSuId });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaKhenThuong(int id)
    {
        var item = await _db.KhenThuongs.FindAsync(id);
        if (item == null || !await _scope.CanViewAsync(User, item.NhanSuId)) return NotFound();
        _db.KhenThuongs.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(KhenThuong), new { nhanSuId = item.NhanSuId });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaKhenThuong(KhenThuongViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();
        if (!ModelState.IsValid || !vm.Id.HasValue) return RedirectToAction(nameof(KhenThuong), new { nhanSuId = vm.NhanSuId });
        var item = await _db.KhenThuongs.FirstOrDefaultAsync(x => x.Id == vm.Id && x.NhanSuId == vm.NhanSuId);
        if (item == null) return NotFound();
        item.Loai = vm.Loai.Trim();
        item.NoiDung = vm.NoiDung.Trim();
        item.Ngay = vm.Ngay!.Value;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(KhenThuong), new { nhanSuId = vm.NhanSuId });
    }

    [HttpGet]
    public async Task<IActionResult> KyLuat(int nhanSuId)
    {
        if (!await _scope.CanViewAsync(User, nhanSuId)) return NotFound();
        var ns = await _db.NhanSus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == nhanSuId);
        if (ns == null) return NotFound();
        return View(new KyLuatViewModel
        {
            NhanSuId = nhanSuId,
            HoTen = ns.HoTen,
            Items = await _db.KyLuats.AsNoTracking()
                .Where(x => x.NhanSuId == nhanSuId)
                .OrderByDescending(x => x.Ngay)
                .ToListAsync()
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> KyLuat(KyLuatViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();
        if (!ModelState.IsValid) return View(vm);
        _db.KyLuats.Add(new KyLuat
        {
            NhanSuId = vm.NhanSuId,
            Loai = vm.Loai.Trim(),
            NoiDung = vm.NoiDung.Trim(),
            Ngay = vm.Ngay!.Value
        });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(KyLuat), new { nhanSuId = vm.NhanSuId });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaKyLuat(int id)
    {
        var item = await _db.KyLuats.FindAsync(id);
        if (item == null || !await _scope.CanViewAsync(User, item.NhanSuId)) return NotFound();
        _db.KyLuats.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(KyLuat), new { nhanSuId = item.NhanSuId });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaKyLuat(KyLuatViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();
        if (!ModelState.IsValid || !vm.Id.HasValue) return RedirectToAction(nameof(KyLuat), new { nhanSuId = vm.NhanSuId });
        var item = await _db.KyLuats.FirstOrDefaultAsync(x => x.Id == vm.Id && x.NhanSuId == vm.NhanSuId);
        if (item == null) return NotFound();
        item.Loai = vm.Loai.Trim();
        item.NoiDung = vm.NoiDung.Trim();
        item.Ngay = vm.Ngay!.Value;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(KyLuat), new { nhanSuId = vm.NhanSuId });
    }

    [HttpGet]
    public async Task<IActionResult> GioGiang(int nhanSuId)
    {
        if (!await _scope.CanViewAsync(User, nhanSuId)) return NotFound();
        var ns = await _db.NhanSus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == nhanSuId);
        if (ns == null) return NotFound();
        return View(new GioGiangViewModel
        {
            NhanSuId = nhanSuId, HoTen = ns.HoTen,
            Items = await _db.GioGiangs.AsNoTracking().Where(x => x.NhanSuId == nhanSuId)
                .OrderByDescending(x => x.NamHoc).ToListAsync()
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> GioGiang(GioGiangViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();
        if (!ModelState.IsValid) return View(vm);
        _db.GioGiangs.Add(new GioGiang { NhanSuId = vm.NhanSuId, NamHoc = vm.NamHoc.Trim(), SoGio = vm.SoGio });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(GioGiang), new { nhanSuId = vm.NhanSuId });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaGioGiang(int id)
    {
        var item = await _db.GioGiangs.FindAsync(id);
        if (item == null || !await _scope.CanViewAsync(User, item.NhanSuId)) return NotFound();
        _db.GioGiangs.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(GioGiang), new { nhanSuId = item.NhanSuId });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaGioGiang(GioGiangViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();
        if (!ModelState.IsValid || !vm.Id.HasValue) return RedirectToAction(nameof(GioGiang), new { nhanSuId = vm.NhanSuId });
        var item = await _db.GioGiangs.FirstOrDefaultAsync(x => x.Id == vm.Id && x.NhanSuId == vm.NhanSuId);
        if (item == null) return NotFound();
        item.NamHoc = vm.NamHoc.Trim();
        item.SoGio = vm.SoGio;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(GioGiang), new { nhanSuId = vm.NhanSuId });
    }

    [HttpGet]
    public async Task<IActionResult> Nckh(int nhanSuId)
    {
        if (!await _scope.CanViewAsync(User, nhanSuId)) return NotFound();
        var ns = await _db.NhanSus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == nhanSuId);
        if (ns == null) return NotFound();
        return View(new NckhViewModel
        {
            NhanSuId = nhanSuId, HoTen = ns.HoTen,
            Items = await _db.HoatDongNCKHs.AsNoTracking().Where(x => x.NhanSuId == nhanSuId)
                .OrderByDescending(x => x.Nam).ToListAsync()
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nckh(NckhViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateNckhViewModelAsync(vm);
            return View(vm);
        }
        _db.HoatDongNCKHs.Add(new HoatDongNCKH { NhanSuId = vm.NhanSuId, TenHoatDong = vm.TenHoatDong.Trim(), Nam = vm.Nam });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm hoạt động nghiên cứu khoa học.";
        return RedirectToAction(nameof(Nckh), new { nhanSuId = vm.NhanSuId });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaNckh(int id)
    {
        var item = await _db.HoatDongNCKHs.FindAsync(id);
        if (item == null || !await _scope.CanViewAsync(User, item.NhanSuId)) return NotFound();
        _db.HoatDongNCKHs.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Nckh), new { nhanSuId = item.NhanSuId });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaNckh(NckhViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();
        if (!ModelState.IsValid || !vm.Id.HasValue) return RedirectToAction(nameof(Nckh), new { nhanSuId = vm.NhanSuId });
        var item = await _db.HoatDongNCKHs.FirstOrDefaultAsync(x => x.Id == vm.Id && x.NhanSuId == vm.NhanSuId);
        if (item == null) return NotFound();
        item.TenHoatDong = vm.TenHoatDong.Trim();
        item.Nam = vm.Nam;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Nckh), new { nhanSuId = vm.NhanSuId });
    }

    private static string CsvEscape(string value) =>
        $"\"{value.Replace("\"", "\"\"")}\"";

    private async Task PopulateNckhViewModelAsync(NckhViewModel vm)
    {
        vm.HoTen = await _db.NhanSus
            .Where(x => x.Id == vm.NhanSuId)
            .Select(x => x.HoTen)
            .FirstOrDefaultAsync() ?? "";
        vm.Items = await _db.HoatDongNCKHs.AsNoTracking()
            .Where(x => x.NhanSuId == vm.NhanSuId)
            .OrderByDescending(x => x.Nam)
            .ToListAsync();
    }

    private async Task<bool> CanViewSensitiveAsync(int id)
    {
        if (User.IsInRole(Roles.Hr) || User.IsInRole(Roles.Admin)) return await _scope.CanViewAsync(User, id);
        var current = await _users.GetUserAsync(User);
        return current?.NhanSuId == id;
    }

    private async Task<NangLuongViewModel> BuildLuongVmAsync(int id)
    {
        var ns = await _db.NhanSus.AsNoTracking().FirstAsync(x => x.Id == id);
        return new NangLuongViewModel
        {
            NhanSuId = id, HoTen = ns.HoTen,
            Items = await _db.NangLuongs.AsNoTracking().Where(x => x.NhanSuId == id)
                .OrderByDescending(x => x.NgayHieuLuc).ToListAsync(),
            NgayHieuLuc = DateTime.Today
        };
    }
}
