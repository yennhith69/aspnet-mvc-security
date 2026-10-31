using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvcSecurity.Models;

namespace mvcSecurity.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly HoSoScopeService _scope;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db, HoSoScopeService scope)
        {
            _logger = logger;
            _db = db;
            _scope = scope;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeDashboardViewModel
            {
                IsAuthenticated = User.Identity?.IsAuthenticated == true
            };

            if (!model.IsAuthenticated)
                return View(model);

            var scoped = await _scope.ScopedAsync(User);
            var ids = scoped.Select(x => x.Id);
            var today = DateTime.Today;

            model.EmployeeCount = await scoped.CountAsync();
            model.ConfirmedProfileCount = await scoped.CountAsync(x => x.TrangThaiHoSo == "DaXacNhan");
            model.ExpiringContractCount = await _db.HopDongs.CountAsync(x =>
                ids.Contains(x.NhanSuId) &&
                x.TrangThai == "HieuLuc" &&
                x.DenNgay != null &&
                x.DenNgay >= today &&
                x.DenNgay <= today.AddDays(60));
            model.EvidenceCount = await _db.TepMinhChungs.CountAsync(x => ids.Contains(x.NhanSuId));
            model.RecentEmployees = await scoped
                .OrderByDescending(x => x.NgayTao)
                .Take(5)
                .Select(x => new HomeEmployeeRow
                {
                    EmployeeCode = x.MaHrm,
                    FullName = x.HoTen,
                    UnitName = x.DonVi!.Ten,
                    ProfileStatus = x.TrangThaiHoSo
                })
                .ToListAsync();

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PublicDirectory(string? q)
        {
            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim()[..Math.Min(q.Trim().Length, 100)];
            var query =
                from n in _db.NhanSus.AsNoTracking()
                join d in _db.DonVis.AsNoTracking() on n.DonViId equals d.Id
                join c in _db.ChucVus.AsNoTracking() on n.ChucVuId equals c.Id into cj
                from c in cj.DefaultIfEmpty()
                where n.CongKhai && n.TrangThaiHoSo == "DaXacNhan" &&
                      n.TinhTrangCongTac != "NghiViec"
                select new PublicProfileListItem
                {
                    Id = n.Id, MaHrm = n.MaHrm, HoTen = n.HoTen,
                    DonVi = d.Ten, ChucVu = c == null ? null : c.Ten
                };

            if (q != null)
                query = query.Where(x => x.HoTen.Contains(q) || x.MaHrm.Contains(q) || x.DonVi.Contains(q));

            return View(new PublicProfileDirectoryViewModel
            {
                Query = q,
                Items = await query.OrderBy(x => x.HoTen).Take(100).ToListAsync()
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PublicProfile(int id)
        {
            var profile = await (
                from n in _db.NhanSus.AsNoTracking()
                join d in _db.DonVis.AsNoTracking() on n.DonViId equals d.Id
                join c in _db.ChucVus.AsNoTracking() on n.ChucVuId equals c.Id into cj
                from c in cj.DefaultIfEmpty()
                where n.Id == id && n.CongKhai && n.TrangThaiHoSo == "DaXacNhan" &&
                      n.TinhTrangCongTac != "NghiViec"
                select new PublicProfileViewModel
                {
                    MaHrm = n.MaHrm, HoTen = n.HoTen, DonVi = d.Ten,
                    ChucVu = c == null ? null : c.Ten
                }).SingleOrDefaultAsync();

            if (profile == null) return NotFound();
            profile.TrinhDos = await (
                from t in _db.TrinhDos.AsNoTracking()
                join n in _db.NhanSus.AsNoTracking() on t.NhanSuId equals n.Id
                where n.Id == id
                select new PublicEducationRow
                {
                    Loai = t.Loai, ChuyenNganh = t.ChuyenNganh, DonViCap = t.DonViCap
                }).ToListAsync();
            profile.HoatDongNckh = await _db.HoatDongNCKHs.AsNoTracking()
                .Where(x => x.NhanSuId == id)
                .OrderByDescending(x => x.Nam)
                .ThenBy(x => x.TenHoatDong)
                .Select(x => new PublicResearchRow
                {
                    TenHoatDong = x.TenHoatDong,
                    Nam = x.Nam
                })
                .ToListAsync();
            return View(profile);
        }

        // Action xử lý ngoại lệ tập trung: Che giấu toàn bộ Stack Trace khỏi Client (Mục 24)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Xảy ra lỗi hệ thống được chuyển hướng về trang Error. RequestId: {RequestId}", requestId);

            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}