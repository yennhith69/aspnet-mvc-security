using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mvcSecurity.Models;

namespace mvcSecurity.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
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