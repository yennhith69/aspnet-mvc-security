using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

public static class Permissions
{
    public const string HoSoXem = "HoSo.Xem";
    public const string HoSoSua = "HoSo.Sua";
    public const string HoSoXacNhan = "HoSo.XacNhan";
    public const string GiayToXem = "GiayTo.Xem";
    public const string LuongXem = "Luong.Xem";
    public const string BaoCaoIn = "BaoCao.In";
    public const string AdminQuanTri = "Admin.QuanTri";

    public static readonly string[] All =
        { HoSoXem, HoSoSua, HoSoXacNhan, GiayToXem, LuongXem, BaoCaoIn, AdminQuanTri };
}

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionHandler> _logger;

    public PermissionHandler(ApplicationDbContext db, IMemoryCache cache, ILogger<PermissionHandler> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;

        var roles = context.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        var cacheKey = $"permission:{requirement.Permission}:{string.Join("|", roles)}";

        var allowed = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return await _db.VaiTroQuyens.AsNoTracking()
                .AnyAsync(v => roles.Contains(v.RoleName) && v.Quyen!.Ma == requirement.Permission);
        });

        if (allowed)
            context.Succeed(requirement);
        else
            _logger.LogWarning("Truy cập bị từ chối: {User} thiếu quyền {Permission}",
                context.User.Identity?.Name, requirement.Permission);   // mục 15
    }
}