using GameServerAdmin.Application.Posts;
using GameServerAdmin.Domain.Admins;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Backoffice.AdminApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Backoffice
{
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    [Route("api/admins")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _dbContext;

        public AdminController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "댓글 관리";
            return View();
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
