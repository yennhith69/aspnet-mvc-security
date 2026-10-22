using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Policy = Permissions.HoSoXem)]
public class NhanSuController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthorizationService _auth;
    private readonly ILogger<NhanSuController> _logger;

    public NhanSuController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
        IAuthorizationService auth, ILogger<NhanSuController> logger)
    {
        _db = db;
        _userManager = userManager;
        _auth = auth;
        _logger = logger;
    }

    // ---------- Danh sách, tìm kiếm, phân trang (mục 10, 18, 19) ----------

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int? donViId, string? tinhTrang, string? trangThai,
        string sort = "ma", string dir = "asc", int page = 1)
    {
        const int pageSize = 10;
        IQueryable<NhanSu> query = await ScopedAsync();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            if (q.Length > 100) q = q[..100];
            query = query.Where(x => x.HoTen.Contains(q) || x.MaHrm.Contains(q));   // EF tham số hóa
        }
        else q = null;

        if (donViId.HasValue) query = query.Where(x => x.DonViId == donViId.Value);

        if (!string.IsNullOrEmpty(tinhTrang) && NhanSuText.TinhTrangs.Contains(tinhTrang))
            query = query.Where(x => x.TinhTrangCongTac == tinhTrang);
        else tinhTrang = null;

        if (!string.IsNullOrEmpty(trangThai) && NhanSuText.TrangThais.Contains(trangThai))
            query = query.Where(x => x.TrangThaiHoSo == trangThai);
        else trangThai = null;

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        // Chỉ nhận cột sắp xếp nằm trong danh sách cho phép
        var desc = dir == "desc";
        if (!desc) dir = "asc";
        query = sort switch
        {
            "ten" => desc ? query.OrderByDescending(x => x.HoTen) : query.OrderBy(x => x.HoTen),
            "donvi" => desc ? query.OrderByDescending(x => x.DonVi!.Ten) : query.OrderBy(x => x.DonVi!.Ten),
            "ngay" => desc ? query.OrderByDescending(x => x.NgayTao) : query.OrderBy(x => x.NgayTao),
            _ => desc ? query.OrderByDescending(x => x.MaHrm) : query.OrderBy(x => x.MaHrm)
        };
        if (sort != "ten" && sort != "donvi" && sort != "ngay") sort = "ma";

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new NhanSuListItem
            {
                Id = x.Id,
                MaHrm = x.MaHrm,
                HoTen = x.HoTen,
                DonVi = x.DonVi!.Ten,
                TinhTrangCongTac = x.TinhTrangCongTac,
                TrangThaiHoSo = x.TrangThaiHoSo,
                CongKhai = x.CongKhai
            }).ToListAsync();

        var vm = new NhanSuIndexViewModel
        {
            Items = items,
            DonVis = await _db.DonVis.AsNoTracking().OrderBy(x => x.Ten).ToListAsync(),
            Q = q,
            DonViId = donViId,
            TinhTrang = tinhTrang,
            TrangThai = trangThai,
            Sort = sort,
            Dir = dir,
            PageNumber = page,
            TotalPages = totalPages,
            Total = total,
            CoQuyenSua = (await _auth.AuthorizeAsync(User, Permissions.HoSoSua)).Succeeded
        };
        return View(vm);
    }

    // ---------- Chi tiết (mục 18: chỉ xem hồ sơ trong phạm vi của mình) ----------

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var scoped = await ScopedAsync();
        var ns = await scoped.Include(x => x.DonVi).FirstOrDefaultAsync(x => x.Id == id);
        if (ns == null) return NotFound();     // ngoài phạm vi thì không lộ hồ sơ có tồn tại hay không

        var coXemGiayTo = (await _auth.AuthorizeAsync(User, Permissions.GiayToXem)).Succeeded;
        var giayTos = await _db.GiayToDinhDanhs.AsNoTracking()
            .Where(x => x.NhanSuId == ns.Id)
            .Select(x => new GiayToView { Loai = x.Loai, So = x.So })
            .ToListAsync();
        if (!coXemGiayTo)
            foreach (var g in giayTos) g.So = Mask(g.So);      // che số giấy tờ với vai trò không có quyền

        string? chucVu = ns.ChucVuId.HasValue
            ? await _db.ChucVus.AsNoTracking().Where(x => x.Id == ns.ChucVuId).Select(x => x.Ten).FirstOrDefaultAsync()
            : null;

        return View(new NhanSuDetailsViewModel
        {
            NhanSu = ns,
            DonViTen = ns.DonVi?.Ten ?? "",
            ChucVuTen = chucVu,
            GiayTos = giayTos,
            CoQuyenSua = (await _auth.AuthorizeAsync(User, Permissions.HoSoSua)).Succeeded,
            CoQuyenXacNhan = (await _auth.AuthorizeAsync(User, Permissions.HoSoXacNhan)).Succeeded
        });
    }

    // ---------- Tiếp nhận hồ sơ (F1) ----------

    [HttpGet]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View(new NhanSuCreateViewModel());
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Create(NhanSuCreateViewModel vm)
    {
        // Không tin dữ liệu từ form: kiểm tra lại mọi giá trị chọn
        if (vm.NgaySinh.HasValue && (vm.NgaySinh.Value.Year < 1930 || vm.NgaySinh.Value > DateTime.Today.AddYears(-18)))
            ModelState.AddModelError(nameof(vm.NgaySinh), "Ngày sinh không hợp lệ (phải đủ 18 tuổi).");
        if (!NhanSuText.GioiTinhs.Contains(vm.GioiTinh))
            ModelState.AddModelError(nameof(vm.GioiTinh), "Giới tính không hợp lệ.");
        if (!NhanSuText.LoaiGiayTos.Contains(vm.LoaiGiayTo))
            ModelState.AddModelError(nameof(vm.LoaiGiayTo), "Loại giấy tờ không hợp lệ.");
        if (vm.DonViId.HasValue && !await _db.DonVis.AnyAsync(x => x.Id == vm.DonViId.Value))
            ModelState.AddModelError(nameof(vm.DonViId), "Đơn vị không tồn tại.");
        if (vm.ChucVuId.HasValue && !await _db.ChucVus.AnyAsync(x => x.Id == vm.ChucVuId.Value))
            ModelState.AddModelError(nameof(vm.ChucVuId), "Chức vụ không tồn tại.");

        var hoTen = vm.HoTen.Trim();
        var so = vm.SoGiayTo.Trim().ToUpperInvariant();

        // Kiểm tra trùng
        if (ModelState.IsValid)
        {
            if (await _db.GiayToDinhDanhs.AnyAsync(x => x.Loai == vm.LoaiGiayTo && x.So == so))
                ModelState.AddModelError(nameof(vm.SoGiayTo), "Số giấy tờ này đã có trong hệ thống.");

            if (!vm.XacNhanKhongTrung && await _db.NhanSus.AnyAsync(x => x.HoTen == hoTen && x.NgaySinh == vm.NgaySinh!.Value))
                ModelState.AddModelError("", "Đã có hồ sơ trùng họ tên và ngày sinh. Nếu chắc chắn là hai người khác nhau, hãy tick ô xác nhận bên dưới.");
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return View(vm);
        }

        // Cấp mã duy nhất: thử lại nếu hai người cùng lấy một số
        NhanSu? created = null;
        for (var attempt = 0; attempt < 3 && created == null; attempt++)
        {
            try
            {
                await using var tx = await _db.Database.BeginTransactionAsync();
                var ns = new NhanSu
                {
                    MaHrm = await NextMaHrmAsync(),
                    HoTen = hoTen,
                    NgaySinh = vm.NgaySinh!.Value,
                    GioiTinh = vm.GioiTinh,
                    Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim(),
                    DienThoai = string.IsNullOrWhiteSpace(vm.DienThoai) ? null : vm.DienThoai.Trim(),
                    DiaChiCuTru = string.IsNullOrWhiteSpace(vm.DiaChiCuTru) ? null : vm.DiaChiCuTru.Trim(),
                    DonViId = vm.DonViId!.Value,
                    ChucVuId = vm.ChucVuId,
                    TinhTrangCongTac = "DangCongTac",
                    TrangThaiHoSo = "DaCapMa",
                    CongKhai = false
                };
                _db.NhanSus.Add(ns);
                await _db.SaveChangesAsync();

                _db.GiayToDinhDanhs.Add(new GiayToDinhDanh { NhanSuId = ns.Id, Loai = vm.LoaiGiayTo, So = so });
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                created = ns;
            }
            catch (DbUpdateException ex)
            {
                _db.ChangeTracker.Clear();
                _logger.LogWarning(ex, "Lưu hồ sơ không thành công, thử lại lần {Attempt}", attempt + 1);
            }
        }

        if (created == null)
        {
            ModelState.AddModelError("", "Không thể lưu hồ sơ. Vui lòng thử lại.");
            await LoadListsAsync();
            return View(vm);
        }

        await WriteAuditAsync("TiepNhanHoSo", created.Id, $"Mã {created.MaHrm}");   // không ghi số giấy tờ
        _logger.LogInformation("{User} tiếp nhận hồ sơ {Ma}", User.Identity?.Name, created.MaHrm);

        TempData["Success"] = $"Đã tiếp nhận hồ sơ, mã nhân sự: {created.MaHrm}.";
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    // ---------- Sửa hồ sơ (F2) ----------

    [HttpGet]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Edit(int id)
    {
        var ns = await _db.NhanSus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (ns == null) return NotFound();

        await LoadListsAsync();
        return View(new NhanSuEditViewModel
        {
            Id = ns.Id,
            HoTen = ns.HoTen,
            NgaySinh = ns.NgaySinh,
            GioiTinh = ns.GioiTinh,
            Email = ns.Email,
            DienThoai = ns.DienThoai,
            DiaChiCuTru = ns.DiaChiCuTru,
            DonViId = ns.DonViId,
            ChucVuId = ns.ChucVuId,
            TinhTrangCongTac = ns.TinhTrangCongTac,
            CongKhai = ns.CongKhai
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    public async Task<IActionResult> Edit(NhanSuEditViewModel vm)
    {
        var ns = await _db.NhanSus.FindAsync(vm.Id);
        if (ns == null) return NotFound();

        if (vm.NgaySinh.HasValue && (vm.NgaySinh.Value.Year < 1930 || vm.NgaySinh.Value > DateTime.Today.AddYears(-18)))
            ModelState.AddModelError(nameof(vm.NgaySinh), "Ngày sinh không hợp lệ (phải đủ 18 tuổi).");
        if (!NhanSuText.GioiTinhs.Contains(vm.GioiTinh))
            ModelState.AddModelError(nameof(vm.GioiTinh), "Giới tính không hợp lệ.");
        if (!NhanSuText.TinhTrangs.Contains(vm.TinhTrangCongTac))
            ModelState.AddModelError(nameof(vm.TinhTrangCongTac), "Tình trạng công tác không hợp lệ.");
        if (vm.DonViId.HasValue && !await _db.DonVis.AnyAsync(x => x.Id == vm.DonViId.Value))
            ModelState.AddModelError(nameof(vm.DonViId), "Đơn vị không tồn tại.");
        if (vm.ChucVuId.HasValue && !await _db.ChucVus.AnyAsync(x => x.Id == vm.ChucVuId.Value))
            ModelState.AddModelError(nameof(vm.ChucVuId), "Chức vụ không tồn tại.");
        if (vm.CongKhai && ns.TrangThaiHoSo != "DaXacNhan")
            ModelState.AddModelError(nameof(vm.CongKhai), "Chỉ được công khai hồ sơ đã xác nhận.");

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return View(vm);
        }

        var hoTen = vm.HoTen.Trim();
        var email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();
        var dienThoai = string.IsNullOrWhiteSpace(vm.DienThoai) ? null : vm.DienThoai.Trim();
        var diaChi = string.IsNullOrWhiteSpace(vm.DiaChiCuTru) ? null : vm.DiaChiCuTru.Trim();

        var changes = new List<string>();
        void Track(string name, object? oldV, object? newV) { if (!Equals(oldV, newV)) changes.Add(name); }
        Track("HoTen", ns.HoTen, hoTen);
        Track("NgaySinh", ns.NgaySinh, vm.NgaySinh!.Value);
        Track("GioiTinh", ns.GioiTinh, vm.GioiTinh);
        Track("Email", ns.Email, email);
        Track("DienThoai", ns.DienThoai, dienThoai);
        Track("DiaChiCuTru", ns.DiaChiCuTru, diaChi);
        Track("DonViId", ns.DonViId, vm.DonViId!.Value);
        Track("ChucVuId", ns.ChucVuId, vm.ChucVuId);
        Track("TinhTrangCongTac", ns.TinhTrangCongTac, vm.TinhTrangCongTac);
        Track("CongKhai", ns.CongKhai, vm.CongKhai);

        // Chỉ cập nhật các trường được phép (không đụng MaHrm, TrangThaiHoSo)
        ns.HoTen = hoTen;
        ns.NgaySinh = vm.NgaySinh!.Value;
        ns.GioiTinh = vm.GioiTinh;
        ns.Email = email;
        ns.DienThoai = dienThoai;
        ns.DiaChiCuTru = diaChi;
        ns.DonViId = vm.DonViId!.Value;
        ns.ChucVuId = vm.ChucVuId;
        ns.TinhTrangCongTac = vm.TinhTrangCongTac;
        ns.CongKhai = vm.CongKhai;
        await _db.SaveChangesAsync();

        await WriteAuditAsync("SuaHoSo", ns.Id, changes.Count == 0 ? "Không đổi" : "Trường đổi: " + string.Join(", ", changes));
        _logger.LogInformation("{User} sửa hồ sơ {Ma}", User.Identity?.Name, ns.MaHrm);

        TempData["Success"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Details), new { id = ns.Id });
    }

    // ---------- Xác nhận hồ sơ (F1) ----------

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoXacNhan)]
    public async Task<IActionResult> Confirm(int id)
    {
        var ns = await _db.NhanSus.FindAsync(id);
        if (ns == null) return NotFound();

        if (ns.TrangThaiHoSo != "DaCapMa")
        {
            TempData["Error"] = "Chỉ xác nhận được hồ sơ ở trạng thái 'Đã cấp mã'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ns.TrangThaiHoSo = "DaXacNhan";
        await _db.SaveChangesAsync();
        await WriteAuditAsync("XacNhanHoSo", ns.Id, $"Mã {ns.MaHrm}");
        _logger.LogInformation("{User} xác nhận hồ sơ {Ma}", User.Identity?.Name, ns.MaHrm);

        TempData["Success"] = "Đã xác nhận hồ sơ.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- Hàm hỗ trợ ----------

    // Phạm vi xem hồ sơ theo vai trò (mục 3, 18): HR và Admin xem tất cả,
    // Manager chỉ trong đơn vị mình, Lecturer chỉ hồ sơ của chính mình
    private async Task<IQueryable<NhanSu>> ScopedAsync()
    {
        IQueryable<NhanSu> q = _db.NhanSus.AsNoTracking();
        if (User.IsInRole(Roles.Hr) || User.IsInRole(Roles.Admin)) return q;

        var u = await _userManager.GetUserAsync(User);
        if (User.IsInRole(Roles.Manager) && u?.DonViId != null)
        {
            var dv = u.DonViId.Value;
            return q.Where(x => x.DonViId == dv);
        }
        if (User.IsInRole(Roles.Lecturer) && u?.NhanSuId != null)
        {
            var id = u.NhanSuId.Value;
            return q.Where(x => x.Id == id);
        }
        return q.Where(x => false);
    }

    private async Task<string> NextMaHrmAsync()
    {
        var prefix = $"GV{DateTime.UtcNow.Year}-";
        var last = await _db.NhanSus.Where(x => x.MaHrm.StartsWith(prefix))
            .OrderByDescending(x => x.MaHrm).Select(x => x.MaHrm).FirstOrDefaultAsync();
        var n = 1;
        if (last != null && int.TryParse(last[prefix.Length..], out var v)) n = v + 1;
        return $"{prefix}{n:0000}";
    }

    private async Task LoadListsAsync()
    {
        ViewBag.DonVis = await _db.DonVis.AsNoTracking().OrderBy(x => x.Ten).ToListAsync();
        ViewBag.ChucVus = await _db.ChucVus.AsNoTracking().OrderBy(x => x.Ten).ToListAsync();
    }

    private static string Mask(string s) =>
        s.Length <= 3 ? new string('*', s.Length) : new string('*', s.Length - 3) + s[^3..];

    private async Task WriteAuditAsync(string hanhDong, int doiTuongId, string? chiTiet)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _userManager.GetUserId(User),
            UserName = User.Identity?.Name ?? "",
            HanhDong = hanhDong,
            DoiTuong = "NhanSu",
            DoiTuongId = doiTuongId.ToString(),
            ChiTiet = chiTiet,
            Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync();
    }
}