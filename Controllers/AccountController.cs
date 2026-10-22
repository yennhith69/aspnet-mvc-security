using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AccountController> _logger;

    public AccountController(SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    // ---------- Đăng nhập (mục 9, 15, 20) ----------

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim();

        // lockoutOnFailure: true -> sai 5 lần thì khóa 15 phút (cấu hình trong Program.cs)
        var result = await _signInManager.PasswordSignInAsync(
            email, vm.Password, isPersistent: false, lockoutOnFailure: true);

        var user = await _userManager.FindByNameAsync(email);
        var ketQua = result.Succeeded ? "Success" : result.IsLockedOut ? "LockedOut" : "Failed";
        await WriteLoginLogAsync(email, user?.Id, ketQua);

        if (result.Succeeded)
        {
            _logger.LogInformation("Đăng nhập thành công: {User}", email);
            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return LocalRedirect(vm.ReturnUrl);          // chống open redirect
            return RedirectToAction("Index", "Home");
        }

        _logger.LogWarning("Đăng nhập không thành công ({KetQua}): {User}", ketQua, email);

        if (result.IsLockedOut)
        {
            var lockoutEnd = user?.LockoutEnd;

            if (lockoutEnd.HasValue && lockoutEnd.Value > DateTimeOffset.UtcNow.AddYears(1))
            {
                // Khóa do quản trị viên (không có hạn)
                ModelState.AddModelError("", "Tài khoản đã bị quản trị viên khóa. Vui lòng liên hệ quản trị viên để được mở khóa.");
            }
            else
            {
                // Khóa tự động do đăng nhập sai nhiều lần: báo số phút còn lại
                var minutes = lockoutEnd.HasValue
                    ? (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes)
                    : 15;
                if (minutes < 1) minutes = 1;
                ModelState.AddModelError("", $"Tài khoản tạm thời bị khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau khoảng {minutes} phút.");
            }
        }
        else
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
        }

        vm.Password = "";
        return View(vm);
    }

    // ---------- Đăng xuất (mục 12: chỉ nhận POST) ----------

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("Đăng xuất: {User}", User.Identity?.Name);
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    // ---------- Đổi mật khẩu (mục 17) ----------

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Không cho dùng lại mật khẩu hiện tại hoặc 3 mật khẩu gần nhất
        var hasher = _userManager.PasswordHasher;
        var oldHashes = await _db.LichSuMatKhaus
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.NgayDoi)
            .Take(3)
            .Select(x => x.PasswordHash)
            .ToListAsync();
        if (!string.IsNullOrEmpty(user.PasswordHash)) oldHashes.Add(user.PasswordHash);

        foreach (var h in oldHashes)
        {
            if (hasher.VerifyHashedPassword(user, h, vm.NewPassword) != PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(nameof(vm.NewPassword), "Không được dùng lại mật khẩu hiện tại hoặc 3 mật khẩu gần nhất.");
                return View(vm);
            }
        }

        var oldHash = user.PasswordHash;   // hash của mật khẩu hiện tại, sắp bị thay

        // Identity kiểm tra mật khẩu hiện tại và chính sách mật khẩu mới
        var result = await _userManager.ChangePasswordAsync(user, vm.CurrentPassword, vm.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
            {
                var msg = e.Code == "PasswordMismatch" ? "Mật khẩu hiện tại không đúng." : e.Description;
                ModelState.AddModelError("", msg);
            }
            _logger.LogWarning("Đổi mật khẩu không thành công: {User}", user.UserName);
            return View(vm);
        }

        // Lưu hash của mật khẩu CŨ vào lịch sử (không lưu mật khẩu thô) và ghi nhật ký thao tác
        if (!string.IsNullOrEmpty(oldHash))
            _db.LichSuMatKhaus.Add(new LichSuMatKhau { UserId = user.Id, PasswordHash = oldHash });

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            UserName = user.UserName ?? "",
            HanhDong = "DoiMatKhau",
            DoiTuong = "TaiKhoan",
            DoiTuongId = user.Id,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync();

        await _signInManager.RefreshSignInAsync(user);   // giữ phiên đăng nhập sau khi đổi
        _logger.LogInformation("Đổi mật khẩu thành công: {User}", user.UserName);

        TempData["Success"] = "Đổi mật khẩu thành công.";
        return RedirectToAction(nameof(ChangePassword));
    }

    // ---------- Trang từ chối truy cập ----------

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Denied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    // ---------- Ghi nhật ký đăng nhập (mục 15, 20): không ghi mật khẩu ----------

    private async Task WriteLoginLogAsync(string userName, string? userId, string ketQua)
    {
        _db.LoginLogs.Add(new LoginLog
        {
            UserName = userName.Length > 256 ? userName[..256] : userName,
            UserId = userId,
            KetQua = ketQua,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync();
    }
}