using GameServerAdmin.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace GameServerAdmin.Infrastructure.Persistence
{
    public class DbInitializer
    {
        public static async Task SeedRolesAsync(RoleManager<AppRole> roleManager)
        {
            var roles = new[] { "User", "Admin", "SuperAdmin" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new AppRole(roleName));
                }
            }
        }

        public static async Task SeedAdminUserAsync(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager)
        {
            const string adminUserName = "admin";
            const string adminPassword = "testPassword1"; // ⚠ 개발용. 실서비스에선 환경변수 권장
            const string adminRole = "Admin";

            // 1️⃣ Admin Role 없으면 생성 (안전 보강)
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new AppRole(adminRole));
            }

            // 2️⃣ Admin 계정 존재 여부 확인
            var adminUser = await userManager.FindByNameAsync(adminUserName);
            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = adminUserName,
                    // Email 필수면 추가:
                    // Email = "admin@example.com",
                    // EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new Exception($"Admin user creation failed: {errors}");
                }
            }

            // 3️⃣ Admin Role 부여
            var roles = await userManager.GetRolesAsync(adminUser);
            if (!roles.Contains(adminRole))
            {
                var roleResult = await userManager.AddToRoleAsync(adminUser, adminRole);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Admin role assignment failed: {errors}");
                }
            }
        }
    }
}