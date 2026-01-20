using GameServerAdmin.Domain.Admins;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Admin;
using GameServerAdmin.Models.Admins;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers
{
    [ApiController]
    [Route("api/admins")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        public AdminController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<ActionResult<AdminResponse>> CreateAdmin([FromBody] AdminCreateRequest request)
        {
            var admin = new Admin
            {
                LoginId = request.LoginId,
                PasswordHash = HashPassword(request.Password),
                Role = request.Role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _dbContext.Admins.Add(admin);
            await _dbContext.SaveChangesAsync();
            return Ok(new AdminResponse
            {
                AdminId = admin.AdminId,
                LoginId = admin.LoginId,
                Role = admin.Role,
                CreatedAt = admin.CreatedAt
            });
        }

        // TEMP
        private string HashPassword(string password)
        {
            return null;
        }
    }
}
