using GameServerAdmin.Domain.Game.Inventory;
using GameServerAdmin.Domain.Game.Stages;
using GameServerAdmin.Domain.Identity;
using GameServerAdmin.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

        public static async Task SeedDefaultUserAsync(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            const string userRole = "User";
            const string userName = "user";
            const string userPassword = "testUser123";

            if (await roleManager.RoleExistsAsync(userRole) == false)
            {
                await roleManager.CreateAsync(new AppRole(userRole));
            }

            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
            {
                user = new AppUser
                {
                    UserName = userName,
                };

                var createResult = await userManager.CreateAsync(user, userPassword);
                if (createResult.Succeeded == false)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new Exception($"Default user creation failed : {errors}");
                }
            }

            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Contains(userRole))
            {
                var roleResult = await userManager.AddToRoleAsync(user, userRole);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Default user role assignment failed: {errors}");
                }
            }
        }

        public static async Task SeedAdminUserAsync(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
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
                    UserType = "Admin",
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
        public static async Task SeedStagesAsync(AppDbContext db)
        {
            if (await db.Set<Stage>().AnyAsync()) return; // 이미 있으면 스킵

            var stages = new[]
            {
                new Stage(1, "초원 1구역", requiredStamina: 3, rewardGold: 100, rewardGem: 1),
                new Stage(2, "초원 2구역", requiredStamina: 5, rewardGold: 200, rewardGem: 2),
                new Stage(3, "동굴 입구",  requiredStamina: 8, rewardGold: 400, rewardGem: 5),
            };

            db.Set<Stage>().AddRange(stages);
            await db.SaveChangesAsync();
        }

        public static async Task SeedTestUsersAsync(UserManager<AppUser> userManager, AppDbContext db)
        {
            // 이미 테스트 유저가 있으면 스킵
            if (await userManager.FindByNameAsync("testuser1") != null) return;

            var testUsers = new[]
            {
        new { UserName = "testuser1", Nickname = "용사홍길동", Level = 15L, Status = UserStatus.Active  },
        new { UserName = "testuser2", Nickname = "초보모험가",  Level = 3L,  Status = UserStatus.Active  },
        new { UserName = "testuser3", Nickname = "제재된유저",  Level = 8L,  Status = UserStatus.Banned  },
    };

            foreach (var t in testUsers)
            {
                // 1) AppUser 생성
                var appUser = new AppUser
                {
                    UserName = t.UserName,
                    NickName = t.Nickname,
                    UserType = "User",
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(appUser, "testUser123");
                if (!result.Succeeded) continue;

                await userManager.AddToRoleAsync(appUser, "User");

                // 2) 도메인 User 생성
                var domainUser = new User
                {
                    AccountId = appUser.Id,
                    Nickname = t.Nickname,
                    Level = t.Level,
                    Status = t.Status,
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(domainUser);
                await db.SaveChangesAsync();

                // 3) 재화 지급
                var currencies = t.UserName switch
                {
                    "testuser1" => new[]
                    {
                new PlayerCurrency(domainUser.UserId, CurrencyType.Gold,    5000),
                new PlayerCurrency(domainUser.UserId, CurrencyType.Gem,      100),
                new PlayerCurrency(domainUser.UserId, CurrencyType.Stamina,   20),
            },
                    "testuser2" => new[]
                    {
                new PlayerCurrency(domainUser.UserId, CurrencyType.Gold,    200),
                new PlayerCurrency(domainUser.UserId, CurrencyType.Stamina,   5),
            },
                    "testuser3" => new[]
                    {
                new PlayerCurrency(domainUser.UserId, CurrencyType.Gold,   1500),
                new PlayerCurrency(domainUser.UserId, CurrencyType.Gem,      50),
            },
                    _ => Array.Empty<PlayerCurrency>()
                };

                db.Set<PlayerCurrency>().AddRange(currencies);
                await db.SaveChangesAsync();
            }
        }

    }
}