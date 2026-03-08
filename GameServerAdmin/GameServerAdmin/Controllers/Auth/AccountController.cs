using GameServerAdmin.Domain.Identity;
using GameServerAdmin.Domain.Users;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Controllers.Auth
{
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _db;

        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, AppDbContext db)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _db = db;
        }

        [AllowAnonymous]
        [HttpGet("/account/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View("~/Views/Account/Login.cshtml");
        }

        [AllowAnonymous]
        [HttpPost("/account/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(GameServerAdmin.Models.Auth.LoginRequest request, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View("~/Views/Account/Login.cshtml", request);

            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "아이디 또는 비밀번호가 올바르지 않습니다");
                return View("~/Views/Account/Login.cshtml", request);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, request.Password, isPersistent: true, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "아이디 또는 비밀번호가 올바르지 않습니다");
                return View("~/Views/Account/Login.cshtml", request);
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Board");
        }

        [Authorize]
        [HttpPost("/account/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Board");
        }

        // 🔹 회원가입 화면 보여주기
        [AllowAnonymous]
        [HttpGet("/account/register")]
        public IActionResult Register()
        {
            return View("~/Views/Account/Register.cshtml", new GameServerAdmin.Models.Auth.RegisterRequest());
        }

        [AllowAnonymous]
        [HttpPost("/account/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Models.Auth.RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Account/Register.cshtml", request);

            var existing = await _userManager.FindByNameAsync(request.UserName);
            if (existing != null)
            {
                ModelState.AddModelError(string.Empty, "이미 사용 중인 아이디입니다.");
                return View("~/Views/Account/Register.cshtml", request);
            }

            // 1) AppUser (Identity) 생성
            var appUser = new AppUser
            {
                UserName = request.UserName,
                NickName = request.UserName,
                CreatedAt = DateTime.UtcNow,
                UserType = "User"
            };

            var result = await _userManager.CreateAsync(appUser, request.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View("~/Views/Account/Register.cshtml", request);
            }

            // 2) 도메인 User 생성 후 AppUser.AccountId 연결
            var domainUser = new User
            {
                AccountId = appUser.Id,   // AppUser.Id로 연결
                Nickname = request.UserName,
                Level = 1,
                CreatedAt = DateTime.UtcNow,
                Status = UserStatus.Active
            };
            _db.Users.Add(domainUser);
            await _db.SaveChangesAsync();

            return RedirectToAction("Login", "Account");
        }


        [AllowAnonymous]
        [HttpGet("/account/denied")]
        public IActionResult Denied() => Content("Access Denied");
    }
}