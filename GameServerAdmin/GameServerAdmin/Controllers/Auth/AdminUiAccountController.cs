using GameServerAdmin.Domain.Admins;
using GameServerAdmin.Domain.Identity;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Auth
{
    public class AdminUiAccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _db;

        public AdminUiAccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, AppDbContext db)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _db = db;
        }

        [AllowAnonymous]
        [HttpGet("/admin/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View("~/Views/AdminAccount/Login.cshtml");
        }

        [AllowAnonymous]
        [HttpPost("/admin/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest request, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View("~/Views/AdminAccount/Login.cshtml", request);

            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "아이디 또는 비밀번호가 올바르지 않습니다");
                return View("~/Views/AdminAccount/Login.cshtml", request);
            }

            // AdminOnly 정책이 Admin/SuperAdmin 요구하므로 동일하게 체크
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin") && !roles.Contains("SuperAdmin"))
            {
                ModelState.AddModelError(string.Empty, "관리자 계정이 아닙니다");
                return View("~/Views/AdminAccount/Login.cshtml", request);
            }

            // Admin 도메인 엔티티 자동 생성
            if(user.AdminId == null)
            {
                var admin = new Admin
                {
                    LoginId = user.UserName!,
                    PasswordHash = string.Empty,
                    Role = roles.Contains("SuperAdmin") ? "SuperAdmin" : "Admin",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _db.Admins.Add(admin);
                await _db.SaveChangesAsync();

                user.AdminId = admin.AdminId;
                user.UserType = "Admin";
                await _userManager.UpdateAsync(user);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, request.Password, isPersistent: true, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "아이디 또는 비밀번호가 올바르지 않습니다");
                return View("~/Views/AdminAccount/Login.cshtml", request);
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Admin 홈(없으면 AdminUiNotice/List로 바꿔도 됨)
            return RedirectToAction("List", "AdminUiNotice");
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("/admin/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/admin/login");
        }
    }
}