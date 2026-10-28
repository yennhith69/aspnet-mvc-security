using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace mvcSecurity.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class NhanSuApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public NhanSuApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? q)
        {
            var query = _db.NhanSus.AsNoTracking().Include(n => n.DonVi).AsQueryable();

            if (User.IsInRole(Roles.Manager))
            {
                var donViIdClaim = User.FindFirst("DonViId")?.Value;
                if (int.TryParse(donViIdClaim, out var dvId))
                {
                    query = query.Where(x => x.DonViId == dvId);
                }
                else
                {
                    return Ok(new List<ApiNhanSuDto>());
                }
            }
            else if (User.IsInRole(Roles.Lecturer))
            {
                var nhanSuIdClaim = User.FindFirst("NhanSuId")?.Value;
                if (int.TryParse(nhanSuIdClaim, out var nsId))
                {
                    query = query.Where(x => x.Id == nsId);
                }
                else
                {
                    return Ok(new List<ApiNhanSuDto>());
                }
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x => x.HoTen.Contains(q) || x.MaHrm.Contains(q));
            }

            var results = await query
                .OrderBy(x => x.MaHrm)
                .Take(100)
                .Select(x => new ApiNhanSuDto
                {
                    Id = x.Id,
                    MaHrm = x.MaHrm,
                    HoTen = x.HoTen,
                    DonVi = x.DonVi != null ? x.DonVi.Ten : string.Empty,
                    TinhTrangCongTac = x.TinhTrangCongTac,
                    TrangThaiHoSo = x.TrangThaiHoSo
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}