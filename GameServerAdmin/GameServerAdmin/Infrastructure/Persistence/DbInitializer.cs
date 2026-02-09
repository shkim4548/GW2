using GameServerAdmin.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace GameServerAdmin.Infrastructure.Persistence
{
    public class DbInitializer
    {
        public static async Task SeedRolesAsync(RoleManager<AppRole> roleManager)
        {
            var roles = new[] { "User", "Admin", "SuperAdmin" };
            foreach(var roleName in roles)
            {
                if(!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new AppRole(roleName));
                }
            }
        }
    }
}
