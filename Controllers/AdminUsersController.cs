using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Policy = Permissions.AdminQuanTri)]
public class AdminUsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(UserManager<ApplicationUser> userManager,
        ApplicationDbContext db, ILogger<AdminUsersController> logger)
    {
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _db.Users.AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.FullName, u.Email, u.LockoutEnd, u.AccessFailedCount })
            .ToListAsync();

        var roleMap = await (from ur in _db.UserRoles
                             join r in _db.Roles on ur.RoleId equals r.Id
                             select new { ur.UserId, r.Name }).ToListAsync();

        var rows = users.Select(u => new UserRowViewModel
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? "",
            Roles = string.Join(", ", roleMap.Where(x => x.UserId == u.Id).Select(x => x.Name)),
            IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
            AccessFailedCount = u.AccessFailedCount
        }).ToList();

        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserViewModel vm)
    {
        // Không tin dữ liệu từ form: vai trò phải nằm trong danh sách cho phép
        if (!Roles.All.Contains(vm.Role))
            ModelState.AddModelError(nameof(vm.Role), "Vai trò không hợp lệ.");
        if (vm.DonViId.HasValue && !await _db.DonVis.AnyAsync(x => x.Id == vm.DonViId))
            ModelState.AddModelError(nameof(vm.DonViId), "Đơn vị không tồn tại.");
        if (vm.NhanSuId.HasValue && !await _db.NhanSus.AnyAsync(x => x.Id == vm.NhanSuId))
            ModelState.AddModelError(nameof(vm.NhanSuId), "Hồ sơ không tồn tại.");

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return View(vm);
        }

        var email = vm.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = vm.FullName.Trim(),
            EmailConfirmed = true,
            DonViId = vm.Role == Roles.Manager ? vm.DonViId : null,
            NhanSuId = vm.Role == Roles.Lecturer ? vm.NhanSuId : null
        };

        var result = await _userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            await LoadListsAsync();
            return View(vm);
        }

        await _userManager.AddToRoleAsync(user, vm.Role);
        await WriteAuditAsync("TaoTaiKhoan", user.Id, $"Vai trò: {vm.Role}");   // không ghi mật khẩu
        _logger.LogInformation("Admin {Admin} tạo tài khoản {User} vai trò {Role}", User.Identity?.Name, email, vm.Role);

        TempData["Success"] = $"Đã tạo tài khoản {email}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Lock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "Không thể tự khóa tài khoản của chính mình.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        await _userManager.UpdateSecurityStampAsync(user);    // hủy các phiên đang đăng nhập
        await WriteAuditAsync("KhoaTaiKhoan", user.Id, null);
        _logger.LogWarning("Admin {Admin} khóa tài khoản {User}", User.Identity?.Name, user.UserName);

        TempData["Success"] = $"Đã khóa {user.Email}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        await WriteAuditAsync("MoKhoaTaiKhoan", user.Id, null);
        _logger.LogInformation("Admin {Admin} mở khóa tài khoản {User}", User.Identity?.Name, user.UserName);

        TempData["Success"] = $"Đã mở khóa {user.Email}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> LoginLogs(string? ketQua, int page = 1)
    {
        const int pageSize = 20;
        var allowed = new[] { "Success", "Failed", "LockedOut" };    // whitelist bộ lọc

        var q = _db.LoginLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(ketQua) && allowed.Contains(ketQua))
            q = q.Where(x => x.KetQua == ketQua);
        else
            ketQua = null;

        if (page < 1) page = 1;
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        ViewBag.KetQua = ketQua;
        return View(items);
    }

    private async Task LoadListsAsync()
    {
        ViewBag.DonVis = await _db.DonVis.AsNoTracking().OrderBy(x => x.Ten).ToListAsync();
        ViewBag.NhanSus = await _db.NhanSus.AsNoTracking().OrderBy(x => x.HoTen)
            .Select(x => new { x.Id, x.MaHrm, x.HoTen }).ToListAsync();
    }

    private async Task WriteAuditAsync(string hanhDong, string doiTuongId, string? chiTiet)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userManager.GetUserId(User),
            UserName = User.Identity?.Name ?? "",
            HanhDong = hanhDong,
            DoiTuong = "TaiKhoan",
            DoiTuongId = doiTuongId,
            ChiTiet = chiTiet,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync();
    }
}