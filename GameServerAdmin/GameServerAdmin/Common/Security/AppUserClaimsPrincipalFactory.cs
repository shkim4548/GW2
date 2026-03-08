using GameServerAdmin.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using GameServerAdmin.Infrastructure.Persistence;

namespace GameServerAdmin.Common.Security;

public sealed class AppUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<AppUser, AppRole>
{
    private readonly AppDbContext _db;

    public AppUserClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IOptions<IdentityOptions> options,
        AppDbContext db)
        : base(userManager, roleManager, options)
    {
        _db = db;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser appUser)
    {
        var identity = await base.GenerateClaimsAsync(appUser);

        // actor_type 클레임 추가
        identity.AddClaim(new Claim("actor_type", appUser.UserType));

        // actor_id: 도메인 User.UserId 조회 후 설정
        if (appUser.UserType == "User")
        {
            var domainUser = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.AccountId == appUser.Id);

            if (domainUser != null)
                identity.AddClaim(new Claim("actor_id", domainUser.UserId.ToString()));
        }
        else if (appUser.UserType == "Admin" && appUser.AdminId.HasValue)
        {
            identity.AddClaim(new Claim("actor_id", appUser.AdminId.Value.ToString()));
        }

        return identity;
    }
}
