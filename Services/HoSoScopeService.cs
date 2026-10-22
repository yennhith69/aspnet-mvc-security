using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class HoSoScopeService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public HoSoScopeService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // HR và Admin xem tất cả; Manager trong đơn vị mình; Lecturer chỉ hồ sơ của chính mình
    public async Task<IQueryable<NhanSu>> ScopedAsync(ClaimsPrincipal user)
    {
        IQueryable<NhanSu> q = _db.NhanSus.AsNoTracking();
        if (user.IsInRole(Roles.Hr) || user.IsInRole(Roles.Admin)) return q;

        var u = await _userManager.GetUserAsync(user);
        if (user.IsInRole(Roles.Manager) && u?.DonViId != null)
        {
            var dv = u.DonViId.Value;
            return q.Where(x => x.DonViId == dv);
        }
        if (user.IsInRole(Roles.Lecturer) && u?.NhanSuId != null)
        {
            var id = u.NhanSuId.Value;
            return q.Where(x => x.Id == id);
        }
        return q.Where(x => false);
    }

    public async Task<bool> CanViewAsync(ClaimsPrincipal user, int nhanSuId) =>
        await (await ScopedAsync(user)).AnyAsync(x => x.Id == nhanSuId);
}