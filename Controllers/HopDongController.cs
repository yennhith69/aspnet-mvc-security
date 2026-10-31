using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Policy = Permissions.HoSoXem)]
public class HopDongController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly HoSoScopeService _scope;
    private readonly IAuthorizationService _auth;
    private readonly ILogger<HopDongController> _logger;

    public HopDongController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
        HoSoScopeService scope, IAuthorizationService auth, ILogger<HopDongController> logger)
    {
        _db = db;
        _userManager = userManager;
        _scope = scope;
        _auth = auth;
        _logger = logger;
    }

    // ---------- Danh sách hợp đồng của một hồ sơ (mục 18) ----------

    [HttpGet]
    public async Task<IActionResult> Index(int nhanSuId)
    {
        var ns = await (await _scope.ScopedAsync(User)).FirstOrDefaultAsync(x => x.Id == nhanSuId);
        if (ns == null) return NotFound();

        var items = await _db.HopDongs.AsNoTracking()
            .Where(x => x.NhanSuId == nhanSuId)
            .OrderByDescending(x => x.TuNgay)
            .ToListAsync();

        return View(new HopDongIndexViewModel
        {
            NhanSuId = ns.Id,
            MaHrm = ns.MaHrm,
            HoTen = ns.HoTen,
            Items = items,
            CoQuyenSua = (await _auth.AuthorizeAsync(User, Permissions.HoSoSua)).Succeeded
        });
    }

    // ---------- Hợp đồng sắp hết hạn (trong phạm vi được xem) ----------

    [HttpGet]
    public async Task<IActionResult> SapHetHan(int days = 60)
    {
        if (days != 30 && days != 60 && days != 90) days = 60;      // whitelist

        var today = DateTime.Today;
        var limit = today.AddDays(days);
        var scoped = await _scope.ScopedAsync(User);

        var rows = await (
            from h in _db.HopDongs.AsNoTracking()
            where h.TrangThai == "HieuLuc" && h.DenNgay != null && h.DenNgay >= today && h.DenNgay <= limit
            join n in scoped on h.NhanSuId equals n.Id
            orderby h.DenNgay
            select new HopDongSapHetHanRow
            {
                Id = h.Id,
                NhanSuId = n.Id,
                MaHrm = n.MaHrm,
                HoTen = n.HoTen,
                Loai = h.Loai,
                DenNgay = h.DenNgay!.Value
            }).Take(200).ToListAsync();                              // giới hạn số dòng (mục 27)

        return View(new HopDongSapHetHanViewModel { Items = rows, Days = days });
    }

    // ---------- Thêm hợp đồng ----------

    [HttpGet]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Create(int nhanSuId)
    {
        if (!await _scope.CanViewAsync(User, nhanSuId)) return NotFound();
        return View(new HopDongCreateViewModel { NhanSuId = nhanSuId, TuNgay = DateTime.Today });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Create(HopDongCreateViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();

        if (!HopDongText.Loais.Contains(vm.Loai))
            ModelState.AddModelError(nameof(vm.Loai), "Loại hợp đồng không hợp lệ.");

        if (vm.TuNgay.HasValue && (vm.TuNgay.Value.Year < 1990 || vm.TuNgay.Value > DateTime.Today.AddYears(1)))
            ModelState.AddModelError(nameof(vm.TuNgay), "Ngày bắt đầu không hợp lệ.");

        DateTime? end = vm.DenNgay;
        if (vm.Loai == "KhongXacDinhThoiHan")
        {
            end = null;                                               // loại này không có ngày kết thúc
        }
        else if (HopDongText.Loais.Contains(vm.Loai))
        {
            if (!end.HasValue)
                ModelState.AddModelError(nameof(vm.DenNgay), "Loại hợp đồng này bắt buộc có ngày kết thúc.");
            else if (vm.TuNgay.HasValue && end.Value < vm.TuNgay.Value)
                ModelState.AddModelError(nameof(vm.DenNgay), "Ngày kết thúc phải sau ngày bắt đầu.");
            else if (vm.TuNgay.HasValue && end.Value > vm.TuNgay.Value.AddYears(10))
                ModelState.AddModelError(nameof(vm.DenNgay), "Thời hạn hợp đồng không được quá 10 năm.");
        }

        // Không cho hai hợp đồng còn hiệu lực chồng thời gian
        if (ModelState.IsValid)
        {
            var start = vm.TuNgay!.Value;
            var newEnd = end ?? DateTime.MaxValue;
            var trung = await _db.HopDongs.AnyAsync(x =>
                x.NhanSuId == vm.NhanSuId && x.TrangThai != "ChamDut" &&
                x.TuNgay <= newEnd && (x.DenNgay == null || x.DenNgay >= start));
            if (trung)
                ModelState.AddModelError("", "Thời gian này trùng với một hợp đồng đang hiệu lực. Hãy chấm dứt hợp đồng cũ trước hoặc chọn khoảng thời gian khác.");
        }

        if (!ModelState.IsValid) return View(vm);

        var hd = new HopDong
        {
            NhanSuId = vm.NhanSuId,
            Loai = vm.Loai,
            TuNgay = vm.TuNgay!.Value,
            DenNgay = end,
            TrangThai = "HieuLuc"
        };
        _db.HopDongs.Add(hd);
        await _db.SaveChangesAsync();

        await WriteAuditAsync("ThemHopDong", hd.NhanSuId, $"HopDong {hd.Id}, loại {hd.Loai}");
        _logger.LogInformation("{User} thêm hợp đồng cho hồ sơ {Id}", User.Identity?.Name, hd.NhanSuId);

        TempData["Success"] = "Đã thêm hợp đồng.";
        return RedirectToAction(nameof(Index), new { nhanSuId = vm.NhanSuId });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Edit(int id)
    {
        var hd = await _db.HopDongs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (hd == null || !await _scope.CanViewAsync(User, hd.NhanSuId)) return NotFound();
        return View(new HopDongCreateViewModel
        {
            Id = hd.Id, NhanSuId = hd.NhanSuId, Loai = hd.Loai,
            TuNgay = hd.TuNgay, DenNgay = hd.DenNgay
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(HopDongCreateViewModel vm)
    {
        var hd = await _db.HopDongs.FirstOrDefaultAsync(x => x.Id == vm.Id);
        if (hd == null || hd.NhanSuId != vm.NhanSuId ||
            !await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();

        ValidateContract(vm);
        if (ModelState.IsValid)
        {
            var end = vm.Loai == "KhongXacDinhThoiHan" ? null : vm.DenNgay;
            var start = vm.TuNgay!.Value;
            var newEnd = end ?? DateTime.MaxValue;
            var overlap = await _db.HopDongs.AnyAsync(x =>
                x.Id != vm.Id && x.NhanSuId == vm.NhanSuId && x.TrangThai != "ChamDut" &&
                x.TuNgay <= newEnd && (x.DenNgay == null || x.DenNgay >= start));
            if (overlap)
                ModelState.AddModelError("", "Thời gian này trùng với một hợp đồng đang hiệu lực.");
        }

        if (!ModelState.IsValid) return View(vm);

        hd.Loai = vm.Loai;
        hd.TuNgay = vm.TuNgay!.Value;
        hd.DenNgay = vm.Loai == "KhongXacDinhThoiHan" ? null : vm.DenNgay;
        await _db.SaveChangesAsync();
        await WriteAuditAsync("SuaHopDong", hd.NhanSuId, $"HopDong {hd.Id}");
        TempData["Success"] = "Đã cập nhật hợp đồng.";
        return RedirectToAction(nameof(Index), new { nhanSuId = hd.NhanSuId });
    }

    // ---------- Chấm dứt hợp đồng (không xóa) ----------

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Terminate(int id)
    {
        var hd = await _db.HopDongs.FindAsync(id);
        if (hd == null || !await _scope.CanViewAsync(User, hd.NhanSuId)) return NotFound();

        if (hd.TrangThai != "HieuLuc")
        {
            TempData["Error"] = "Hợp đồng này đã được chấm dứt.";
            return RedirectToAction(nameof(Index), new { nhanSuId = hd.NhanSuId });
        }

        hd.TrangThai = "ChamDut";
        await _db.SaveChangesAsync();

        await WriteAuditAsync("ChamDutHopDong", hd.NhanSuId, $"HopDong {hd.Id}");
        _logger.LogWarning("{User} chấm dứt hợp đồng {Id}", User.Identity?.Name, hd.Id);

        TempData["Success"] = "Đã chấm dứt hợp đồng.";
        return RedirectToAction(nameof(Index), new { nhanSuId = hd.NhanSuId });
    }

    private async Task WriteAuditAsync(string hanhDong, int nhanSuId, string? chiTiet)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userManager.GetUserId(User),
            UserName = User.Identity?.Name ?? "",
            HanhDong = hanhDong,
            DoiTuong = "NhanSu",
            DoiTuongId = nhanSuId.ToString(),
            ChiTiet = chiTiet,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync();
    }

    private void ValidateContract(HopDongCreateViewModel vm)
    {
        if (!HopDongText.Loais.Contains(vm.Loai))
            ModelState.AddModelError(nameof(vm.Loai), "Loại hợp đồng không hợp lệ.");
        if (!vm.TuNgay.HasValue)
            return;
        if (vm.TuNgay.Value.Year < 1990 || vm.TuNgay.Value > DateTime.Today.AddYears(1))
            ModelState.AddModelError(nameof(vm.TuNgay), "Ngày bắt đầu không hợp lệ.");
        if (vm.Loai == "KhongXacDinhThoiHan")
            return;
        if (!vm.DenNgay.HasValue)
            ModelState.AddModelError(nameof(vm.DenNgay), "Loại hợp đồng này bắt buộc có ngày kết thúc.");
        else if (vm.DenNgay.Value < vm.TuNgay.Value)
            ModelState.AddModelError(nameof(vm.DenNgay), "Ngày kết thúc phải sau ngày bắt đầu.");
        else if (vm.DenNgay.Value > vm.TuNgay.Value.AddYears(10))
            ModelState.AddModelError(nameof(vm.DenNgay), "Thời hạn hợp đồng không được quá 10 năm.");
    }
}