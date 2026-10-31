using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Policy = Permissions.HoSoXem)]
public class LuongController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly HoSoScopeService _scope;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthorizationService _authorization;

    public LuongController(ApplicationDbContext db, HoSoScopeService scope,
        UserManager<ApplicationUser> userManager, IAuthorizationService authorization)
    {
        _db = db;
        _scope = scope;
        _userManager = userManager;
        _authorization = authorization;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var canViewPlaintext = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager);
        var canManage = (await _authorization.AuthorizeAsync(User, Permissions.HoSoSua)).Succeeded;
        var scoped = await _scope.ScopedAsync(User);

        var items = await (
            from l in _db.NangLuongs.AsNoTracking()
            join n in scoped on l.NhanSuId equals n.Id
            join d in _db.DonVis.AsNoTracking() on n.DonViId equals d.Id
            orderby n.MaHrm, l.NgayHieuLuc descending
            select new LuongReportRow
            {
                NhanSuId = n.Id,
                MaHrm = n.MaHrm,
                HoTen = n.HoTen,
                DonVi = d.Ten,
                Bac = l.Bac,
                MucLuong = l.MucLuong,
                NgayHieuLuc = l.NgayHieuLuc,
                IsMasked = !canViewPlaintext
            }).Take(500).ToListAsync();

        return View(new LuongReportViewModel
        {
            Items = items,
            CanManage = canManage,
            CanViewPlaintext = canViewPlaintext
        });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Create(int nhanSuId)
    {
        if (!await _scope.CanViewAsync(User, nhanSuId)) return NotFound();
        return View(new LuongCreateViewModel { NhanSuId = nhanSuId, NgayHieuLuc = DateTime.Today });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Create(LuongCreateViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();
        if (vm.NgayHieuLuc > DateTime.Today.AddYears(1))
            ModelState.AddModelError(nameof(vm.NgayHieuLuc), "Ngày hiệu lực không hợp lệ.");
        if (!ModelState.IsValid) return View(vm);

        _db.NangLuongs.Add(new NangLuong
        {
            NhanSuId = vm.NhanSuId,
            Bac = vm.Bac.Trim(),
            MucLuong = vm.MucLuong,
            NgayHieuLuc = vm.NgayHieuLuc!.Value
        });
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userManager.GetUserId(User),
            UserName = User.Identity?.Name ?? "",
            HanhDong = "ThemNangLuong",
            DoiTuong = "NhanSu",
            DoiTuongId = vm.NhanSuId.ToString(),
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm quá trình nâng lương.";
        return RedirectToAction(nameof(Index));
    }
}
