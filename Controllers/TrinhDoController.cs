using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Policy = Permissions.HoSoXem)]
public class TrinhDoController : Controller
{
    private const long MaxBytes = 5 * 1024 * 1024;

    // Whitelist phần mở rộng, kiểu nội dung và chữ ký đầu tệp (mục 14, 30)
    private static readonly Dictionary<string, (string Mime, byte[] Sig)> Allowed = new()
    {
        [".pdf"] = ("application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }),
        [".jpg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
        [".jpeg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
        [".png"] = ("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })
    };

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly HoSoScopeService _scope;
    private readonly IAuthorizationService _auth;
    private readonly ILogger<TrinhDoController> _logger;
    private readonly string _dir;

    public TrinhDoController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
        HoSoScopeService scope, IAuthorizationService auth, IWebHostEnvironment env,
        ILogger<TrinhDoController> logger)
    {
        _db = db;
        _userManager = userManager;
        _scope = scope;
        _auth = auth;
        _logger = logger;
        // Ngoài wwwroot nên không truy cập trực tiếp được bằng URL
        _dir = Path.Combine(env.ContentRootPath, "App_Data", "minhchung");
    }

    [HttpGet]
    public async Task<IActionResult> Index(int nhanSuId)
    {
        var ns = await (await _scope.ScopedAsync(User)).FirstOrDefaultAsync(x => x.Id == nhanSuId);
        if (ns == null) return NotFound();

        var items = await (
            from t in _db.TrinhDos.AsNoTracking()
            where t.NhanSuId == nhanSuId
            join f in _db.TepMinhChungs.AsNoTracking() on t.TepMinhChungId equals f.Id into fj
            from f in fj.DefaultIfEmpty()
            orderby t.Id descending
            select new TrinhDoRow
            {
                Id = t.Id,
                Loai = t.Loai,
                ChuyenNganh = t.ChuyenNganh,
                DonViCap = t.DonViCap,
                TepId = f == null ? (int?)null : f.Id,
                TenGoc = f == null ? null : f.TenGoc,
                MoTa = f == null ? null : f.MoTa
            }).ToListAsync();

        return View(new TrinhDoIndexViewModel
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
        return View(new TrinhDoCreateViewModel { NhanSuId = nhanSuId });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(TrinhDoCreateViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();

        if (!TrinhDoText.Loais.Contains(vm.Loai))
            ModelState.AddModelError(nameof(vm.Loai), "Loại trình độ không hợp lệ.");

        var coTep = vm.Tep != null && vm.Tep.Length > 0;
        if (coTep)
        {
            var reason = await ValidateFileAsync(vm.Tep!);
            if (reason != null)
            {
                ModelState.AddModelError(nameof(vm.Tep), reason);
                await RejectAsync(vm.NhanSuId, vm.Tep!.FileName, reason);
            }
        }

        if (!ModelState.IsValid) return View(vm);

        TepMinhChung? tep = null;
        string? fullPath = null;

        if (coTep)
        {
            var ext = Path.GetExtension(vm.Tep!.FileName).ToLowerInvariant();
            var storedName = Guid.NewGuid().ToString("N") + ext;      // không tin tên gốc
            Directory.CreateDirectory(_dir);
            fullPath = Path.Combine(_dir, storedName);

            // CreateNew: báo lỗi nếu tệp đã tồn tại, không bao giờ ghi đè
            await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
                await vm.Tep.CopyToAsync(fs);

            tep = new TepMinhChung
            {
                NhanSuId = vm.NhanSuId,
                TenGoc = SafeName(vm.Tep.FileName),
                TenLuu = storedName,
                KichThuoc = vm.Tep.Length,
                MoTa = string.IsNullOrWhiteSpace(vm.MoTa) ? null : vm.MoTa.Trim()
            };
        }

        try
        {
            if (tep != null)
            {
                _db.TepMinhChungs.Add(tep);
                await _db.SaveChangesAsync();
            }
            _db.TrinhDos.Add(new TrinhDo
            {
                NhanSuId = vm.NhanSuId,
                Loai = vm.Loai,
                ChuyenNganh = vm.ChuyenNganh.Trim(),
                DonViCap = vm.DonViCap.Trim(),
                TepMinhChungId = tep?.Id
            });
            await _db.SaveChangesAsync();
        }
        catch
        {
            if (fullPath != null && System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            throw;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userManager.GetUserId(User),
            UserName = User.Identity?.Name ?? "",
            HanhDong = "ThemTrinhDo",
            DoiTuong = "NhanSu",
            DoiTuongId = vm.NhanSuId.ToString(),
            ChiTiet = tep == null ? "Không kèm tệp" : "Kèm tệp minh chứng",
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync();
        _logger.LogInformation("{User} thêm trình độ cho hồ sơ {Id}", User.Identity?.Name, vm.NhanSuId);

        TempData["Success"] = "Đã thêm trình độ.";
        return RedirectToAction(nameof(Index), new { nhanSuId = vm.NhanSuId });
    }

    // Tải tệp qua action có kiểm tra quyền (mục 18, 30)
    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var f = await _db.TepMinhChungs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (f == null || !await _scope.CanViewAsync(User, f.NhanSuId)) return NotFound();

        var stored = Path.GetFileName(f.TenLuu);
        var ext = Path.GetExtension(stored).ToLowerInvariant();
        var path = Path.Combine(_dir, stored);
        if (!Allowed.TryGetValue(ext, out var info) || !System.IO.File.Exists(path)) return NotFound();

        _logger.LogInformation("{User} tải tệp minh chứng {Id}", User.Identity?.Name, id);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(path, info.Mime, f.TenGoc);     // gửi dạng tệp đính kèm
    }

    // ---------- Hàm hỗ trợ ----------

    private async Task<string?> ValidateFileAsync(IFormFile f)
    {
        if (f.Length > MaxBytes) return "Tệp vượt quá 5 MB.";

        var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
        if (!Allowed.TryGetValue(ext, out var info))
            return "Chỉ chấp nhận tệp PDF, JPG hoặc PNG.";

        var header = new byte[8];
        await using var s = f.OpenReadStream();
        var read = await s.ReadAsync(header, 0, header.Length);
        if (read < info.Sig.Length || !header.Take(info.Sig.Length).SequenceEqual(info.Sig))
            return "Nội dung tệp không khớp với phần mở rộng.";

        return null;
    }

    private static string SafeName(string? raw)
    {
        var name = Path.GetFileName(raw ?? "");
        var invalid = Path.GetInvalidFileNameChars();
        name = new string(name.Where(c => !char.IsControl(c) && !invalid.Contains(c)).ToArray()).Trim();
        if (name.Length > 100) name = name[..100];
        return string.IsNullOrEmpty(name) ? "tep" : name;
    }

    private async Task RejectAsync(int nhanSuId, string rawName, string reason)
    {
        var name = SafeName(rawName);
        _logger.LogWarning("Tải tệp bị từ chối: {User}, tệp {Name}, lý do {Reason}",
            User.Identity?.Name, name, reason);

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userManager.GetUserId(User),
            UserName = User.Identity?.Name ?? "",
            HanhDong = "TaiTepTuChoi",
            DoiTuong = "NhanSu",
            DoiTuongId = nhanSuId.ToString(),
            ChiTiet = $"{name}: {reason}",
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync();
    }
}