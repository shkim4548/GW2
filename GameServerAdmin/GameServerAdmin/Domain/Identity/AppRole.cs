using Microsoft.AspNetCore.Identity;

namespace GameServerAdmin.Domain.Identity
{
    public class AppRole : IdentityRole<long>
    {
        public AppRole() { }
        public AppRole(string roleName) : base(roleName) { }
    }
}
