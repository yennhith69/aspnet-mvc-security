using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace mvcSecurity.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [IgnoreAntiforgeryToken]   // API dùng Bearer token, không dùng cookie nên không chịu CSRF
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, ApplicationDbContext db,
            IConfiguration config, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _config = config;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth_policy")]
        public async Task<IActionResult> Login([FromBody] ApiLoginRequest request)
        {
            var email = request.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);

            // lockoutOnFailure: true -> kiểm tra khóa trước, sai mật khẩu thì tính vào bộ đếm (mục 9)
            var result = user == null
                ? Microsoft.AspNetCore.Identity.SignInResult.Failed
                : await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            var ketQua = result.Succeeded ? "Success" : result.IsLockedOut ? "LockedOut" : "Failed";
            _db.LoginLogs.Add(new LoginLog
            {
                UserName = email.Length > 256 ? email[..256] : email,
                UserId = user?.Id,
                KetQua = "Api" + ketQua,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _db.SaveChangesAsync();

            if (!result.Succeeded)
            {
                _logger.LogWarning("API đăng nhập không thành công ({KetQua}): {Email}", ketQua, email);
                // Thông báo chung, không tiết lộ tài khoản có tồn tại hay không
                return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác, hoặc tài khoản đang bị khóa." });
            }

            var userRoles = await _userManager.GetRolesAsync(user!);
            var authClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user!.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            if (user.DonViId.HasValue) authClaims.Add(new Claim("DonViId", user.DonViId.Value.ToString()));
            if (user.NhanSuId.HasValue) authClaims.Add(new Claim("NhanSuId", user.NhanSuId.Value.ToString()));
            foreach (var role in userRoles) authClaims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var expires = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:DurationInMinutes"] ?? "15"));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                expires: expires,
                claims: authClaims,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return Ok(new ApiLoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expires,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Roles = userRoles
            });
        }
    }
}