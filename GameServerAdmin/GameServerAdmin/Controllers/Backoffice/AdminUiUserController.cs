using GameServerAdmin.Application.AdminGame;
using GameServerAdmin.Models.Users.AdminUi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Backoffice
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminUiUserController : Controller
    {
        private readonly IAdminGameUserService _service;

        public AdminUiUserController(IAdminGameUserService service)
        {
            _service = service;
        }

        // GET /admin/ui/users
        [HttpGet("/admin/ui/users")]
        public async Task<IActionResult> Index(
            [FromQuery] string? nickname,
            [FromQuery] long? userId,
            [FromQuery] int page = 1)
        {
            ViewData["Title"] = "유저 관리";

            const int pageSize = 20;
            bool hasSearched = !string.IsNullOrWhiteSpace(nickname) || userId.HasValue;

            AdminUserListViewModel vm;

            if (hasSearched)
            {
                var results = await _service.SearchUsersAsync(nickname, userId);
                vm = new AdminUserListViewModel
                {
                    Nickname = nickname,
                    UserId = userId,
                    HasSearched = hasSearched,
                    Results = results,
                    TotalCount = results.Count,
                };
            }
            else
            {
                var (users, totalCount) = await _service.GetAllUsersAsync(page, pageSize);
                vm = new AdminUserListViewModel
                {
                    HasSearched = false,
                    Results = users,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                };
            }
            return View("~/Views/AdminUser/Index.cshtml", vm);
        }

        // GET /admin/ui/users/{userId}
        [HttpGet("/admin/ui/users/{userId:long}")]
        public async Task<IActionResult> Detail(long userId)
        {
            var overview = await _service.GetUserOverviewAsync(userId);
            return View("~/Views/AdminUser/Detail.cshtml", overview);
        }

        // POST /admin/ui/users/{userId}/ban
        [HttpPost("/admin/ui/users/{userId:long}/ban")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ban(long userId)
        {
            await _service.BanUserAsync(userId);
            return RedirectToAction(nameof(Detail), new { userId });
        }

        // POST /admin/ui/users/{userId:long}/unban
        [HttpPost("/admin/ui/users/{userId:long}/unban")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unban(long userId)
        {
            await _service.UnbanUserAsync(userId);
            return RedirectToAction(nameof(Detail), new { userId });
        }
    }
}
