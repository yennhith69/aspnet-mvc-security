using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Policy = Permissions.HoSoXem)]
public class QuaTrinhCongTacController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly HoSoScopeService _scope;
    private readonly IAuthorizationService _auth;
    private readonly ILogger<QuaTrinhCongTacController> _logger;

    public QuaTrinhCongTacController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
        HoSoScopeService scope, IAuthorizationService auth, ILogger<QuaTrinhCongTacController> logger)
    {
        _db = db;
        _userManager = userManager;
        _scope = scope;
        _auth = auth;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int nhanSuId)
    {
        var ns = await (await _scope.ScopedAsync(User)).FirstOrDefaultAsync(x => x.Id == nhanSuId);
        if (ns == null) return NotFound();

        var items = await (
            from q in _db.QuaTrinhCongTacs.AsNoTracking()
            where q.NhanSuId == nhanSuId
            join d in _db.DonVis.AsNoTracking() on q.DonViId equals d.Id
            join c in _db.ChucVus.AsNoTracking() on q.ChucVuId equals c.Id into cj
            from c in cj.DefaultIfEmpty()
            orderby q.TuNgay descending
            select new CongTacRow
            {
                Id = q.Id,
                DonVi = d.Ten,
                ChucVu = c == null ? null : c.Ten,
                TuNgay = q.TuNgay,
                DenNgay = q.DenNgay
            }).ToListAsync();

        return View(new CongTacIndexViewModel
        {
            NhanSuId = ns.Id,
            MaHrm = ns.MaHrm,
            HoTen = ns.HoTen,
            Items = items,
            CoQuyenSua = (await _auth.AuthorizeAsync(User, Permissions.HoSoSua)).Succeeded
        });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Create(int nhanSuId)
    {
        if (!await _scope.CanViewAsync(User, nhanSuId)) return NotFound();
        await LoadListsAsync();
        return View(new CongTacCreateViewModel { NhanSuId = nhanSuId, TuNgay = DateTime.Today });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Create(CongTacCreateViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();

        if (vm.DonViId.HasValue && !await _db.DonVis.AnyAsync(x => x.Id == vm.DonViId.Value))
            ModelState.AddModelError(nameof(vm.DonViId), "Đơn vị không tồn tại.");
        if (vm.ChucVuId.HasValue && !await _db.ChucVus.AnyAsync(x => x.Id == vm.ChucVuId.Value))
            ModelState.AddModelError(nameof(vm.ChucVuId), "Chức vụ không tồn tại.");

        if (vm.TuNgay.HasValue && (vm.TuNgay.Value.Year < 1980 || vm.TuNgay.Value > DateTime.Today.AddYears(1)))
            ModelState.AddModelError(nameof(vm.TuNgay), "Ngày bắt đầu không hợp lệ.");
        if (vm.TuNgay.HasValue && vm.DenNgay.HasValue && vm.DenNgay.Value < vm.TuNgay.Value)
            ModelState.AddModelError(nameof(vm.DenNgay), "Ngày kết thúc phải sau ngày bắt đầu.");

        // Không cho hai giai đoạn công tác chồng nhau
        if (ModelState.IsValid)
        {
            var start = vm.TuNgay!.Value;
            var newEnd = vm.DenNgay ?? DateTime.MaxValue;
            var trung = await _db.QuaTrinhCongTacs.AnyAsync(x =>
                x.NhanSuId == vm.NhanSuId && x.TuNgay <= newEnd && (x.DenNgay == null || x.DenNgay >= start));
            if (trung)
                ModelState.AddModelError("", "Giai đoạn này trùng với một giai đoạn công tác khác. Hãy kết thúc giai đoạn đang mở trước.");
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return View(vm);
        }

        var ct = new QuaTrinhCongTac
        {
            NhanSuId = vm.NhanSuId,
            DonViId = vm.DonViId!.Value,
            ChucVuId = vm.ChucVuId,
            TuNgay = vm.TuNgay!.Value,
            DenNgay = vm.DenNgay
        };
        _db.QuaTrinhCongTacs.Add(ct);
        await _db.SaveChangesAsync();

        await WriteAuditAsync("ThemCongTac", ct.NhanSuId, $"CongTac {ct.Id}");
        _logger.LogInformation("{User} thêm quá trình công tác cho hồ sơ {Id}", User.Identity?.Name, ct.NhanSuId);

        TempData["Success"] = "Đã thêm quá trình công tác.";
        return RedirectToAction(nameof(Index), new { nhanSuId = vm.NhanSuId });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Edit(int id)
    {
        var ct = await _db.QuaTrinhCongTacs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (ct == null || !await _scope.CanViewAsync(User, ct.NhanSuId)) return NotFound();
        await LoadListsAsync();
        return View(new CongTacCreateViewModel
        {
            Id = ct.Id, NhanSuId = ct.NhanSuId, DonViId = ct.DonViId,
            ChucVuId = ct.ChucVuId, TuNgay = ct.TuNgay, DenNgay = ct.DenNgay
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CongTacCreateViewModel vm)
    {
        var ct = await _db.QuaTrinhCongTacs.FirstOrDefaultAsync(x => x.Id == vm.Id);
        if (ct == null || ct.NhanSuId != vm.NhanSuId ||
            !await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();

        if (!vm.DonViId.HasValue || !await _db.DonVis.AnyAsync(x => x.Id == vm.DonViId.Value))
            ModelState.AddModelError(nameof(vm.DonViId), "Đơn vị không tồn tại.");
        if (vm.ChucVuId.HasValue && !await _db.ChucVus.AnyAsync(x => x.Id == vm.ChucVuId.Value))
            ModelState.AddModelError(nameof(vm.ChucVuId), "Chức vụ không tồn tại.");
        if (vm.TuNgay.HasValue && (vm.TuNgay.Value.Year < 1980 || vm.TuNgay.Value > DateTime.Today.AddYears(1)))
            ModelState.AddModelError(nameof(vm.TuNgay), "Ngày bắt đầu không hợp lệ.");
        if (vm.TuNgay.HasValue && vm.DenNgay.HasValue && vm.DenNgay.Value < vm.TuNgay.Value)
            ModelState.AddModelError(nameof(vm.DenNgay), "Ngày kết thúc phải sau ngày bắt đầu.");
        if (ModelState.IsValid)
        {
            var start = vm.TuNgay!.Value;
            var end = vm.DenNgay ?? DateTime.MaxValue;
            if (await _db.QuaTrinhCongTacs.AnyAsync(x =>
                x.Id != vm.Id && x.NhanSuId == vm.NhanSuId && x.TuNgay <= end &&
                (x.DenNgay == null || x.DenNgay >= start)))
                ModelState.AddModelError("", "Giai đoạn này trùng với một giai đoạn công tác khác.");
        }
        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return View(vm);
        }

        ct.DonViId = vm.DonViId!.Value;
        ct.ChucVuId = vm.ChucVuId;
        ct.TuNgay = vm.TuNgay!.Value;
        ct.DenNgay = vm.DenNgay;
        await _db.SaveChangesAsync();
        await WriteAuditAsync("SuaCongTac", ct.NhanSuId, $"CongTac {ct.Id}");
        TempData["Success"] = "Đã cập nhật quá trình công tác.";
        return RedirectToAction(nameof(Index), new { nhanSuId = ct.NhanSuId });
    }

    // Kết thúc giai đoạn đang mở: đặt ngày kết thúc là hôm nay (không xóa)
    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Close(int id)
    {
        var ct = await _db.QuaTrinhCongTacs.FindAsync(id);
        if (ct == null || !await _scope.CanViewAsync(User, ct.NhanSuId)) return NotFound();

        if (ct.DenNgay.HasValue)
        {
            TempData["Error"] = "Giai đoạn này đã kết thúc.";
        }
        else if (ct.TuNgay.Date > DateTime.Today)
        {
            TempData["Error"] = "Giai đoạn chưa bắt đầu nên chưa thể kết thúc.";
        }
        else
        {
            ct.DenNgay = DateTime.Today;
            await _db.SaveChangesAsync();
            await WriteAuditAsync("KetThucCongTac", ct.NhanSuId, $"CongTac {ct.Id}");
            _logger.LogInformation("{User} kết thúc giai đoạn công tác {Id}", User.Identity?.Name, ct.Id);
            TempData["Success"] = "Đã kết thúc giai đoạn công tác.";
        }
        return RedirectToAction(nameof(Index), new { nhanSuId = ct.NhanSuId });
    }

    private async Task LoadListsAsync()
    {
        ViewBag.DonVis = await _db.DonVis.AsNoTracking().OrderBy(x => x.Ten).ToListAsync();
        ViewBag.ChucVus = await _db.ChucVus.AsNoTracking().OrderBy(x => x.Ten).ToListAsync();
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
}