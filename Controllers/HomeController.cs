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

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
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
                    MaHrm = n.MaHrm,
                    HoTen = n.HoTen,
                    DonVi = d.Ten,
                    ChucVu = c == null ? null : c.Ten
                }).SingleOrDefaultAsync();

            if (profile == null) return NotFound();
            profile.TrinhDos = await _db.TrinhDos.AsNoTracking()
                .Where(x => _db.NhanSus.Any(n => n.Id == x.NhanSuId && n.MaHrm == profile.MaHrm))
                .Select(x => new PublicEducationRow
                {
                    Loai = x.Loai,
                    ChuyenNganh = x.ChuyenNganh,
                    DonViCap = x.DonViCap
                }).ToListAsync();
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