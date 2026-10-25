using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public AppClaimsPrincipalFactory(UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager, IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (user.DonViId.HasValue) identity.AddClaim(new Claim("DonViId", user.DonViId.Value.ToString()));
        if (user.NhanSuId.HasValue) identity.AddClaim(new Claim("NhanSuId", user.NhanSuId.Value.ToString()));
        return identity;
    }
}