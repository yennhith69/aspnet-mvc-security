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
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _config = config;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth_policy")] // Giới hạn tối đa 10 lần/phút (Mục 23)
        public async Task<IActionResult> Login([FromBody] ApiLoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });
            }

            // Kiểm tra trạng thái khóa tài khoản chuẩn Identity
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                return Unauthorized(new { message = "Tài khoản hiện đang bị tạm khóa." });
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (user.DonViId.HasValue)
            {
                authClaims.Add(new Claim("DonViId", user.DonViId.Value.ToString()));
            }

            if (user.NhanSuId.HasValue)
            {
                authClaims.Add(new Claim("NhanSuId", user.NhanSuId.Value.ToString()));
            }

            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var expires = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:DurationInMinutes"] ?? "60"));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                expires: expires,
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

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