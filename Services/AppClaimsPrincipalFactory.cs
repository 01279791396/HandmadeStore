using System.Security.Claims;
using HandmadeStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HandmadeStore.Services;

/// <summary>بيضيف الاسم الكامل للمستخدم في الكوكي عشان يظهر في الهيدر من غير استعلام</summary>
public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public const string FullNameClaim = "FullName";

    public AppClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(FullNameClaim, string.IsNullOrWhiteSpace(user.Name) ? (user.UserName ?? "") : user.Name));
        return identity;
    }
}
