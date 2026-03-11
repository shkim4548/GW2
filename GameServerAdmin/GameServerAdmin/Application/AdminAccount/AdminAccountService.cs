using GameServerAdmin.Domain.Admins;
using GameServerAdmin.Domain.Identity;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Admin.AdminUi;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.AdminAccount;

public interface IAdminAccountService
{
    Task<List<AdminAccountListItemViewModel>> GetAllAsync();
    Task<(bool Success, string? Error)> CreateAsync(CreateAdminViewModel model);
    Task SetActiveAsync(long adminId, bool isActive);
}

public sealed class AdminAccountService : IAdminAccountService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public AdminAccountService(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<List<AdminAccountListItemViewModel>> GetAllAsync()
    {
        return await _db.Admins
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AdminAccountListItemViewModel
            {
                AdminId = a.AdminId,
                LoginId = a.LoginId,
                Role = a.Role,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                LastLoginAt = a.LastLoginAt
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> CreateAsync(CreateAdminViewModel model)
    {
        // 중복 확인
        if (await _userManager.FindByNameAsync(model.UserName) != null)
            return (false, "이미 존재하는 아이디입니다.");

        // AppUser 생성
        var appUser = new AppUser
        {
            UserName = model.UserName,
            UserType = "Admin"
        };

        var result = await _userManager.CreateAsync(appUser, model.Password);
        if (!result.Succeeded)
        {
            var error = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, error);
        }

        await _userManager.AddToRoleAsync(appUser, "Admin");

        // Admin 도메인 엔티티 생성
        var admin = new Admin
        {
            LoginId = model.UserName,
            PasswordHash = string.Empty,
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _db.Admins.Add(admin);
        await _db.SaveChangesAsync();

        // AppUser ↔ Admin 연결
        appUser.AdminId = admin.AdminId;
        await _userManager.UpdateAsync(appUser);

        return (true, null);
    }

    public async Task SetActiveAsync(long adminId, bool isActive)
    {
        var admin = await _db.Admins.FindAsync(adminId);
        if (admin == null) return;

        admin.IsActive = isActive;
        await _db.SaveChangesAsync();

        // AppUser도 LockoutEnd 처리
        var appUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.AdminId == adminId);
        if (appUser != null)
        {
            await _userManager.SetLockoutEndDateAsync(
                appUser,
                isActive ? null : DateTimeOffset.MaxValue);
        }
    }
}
