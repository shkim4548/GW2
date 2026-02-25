using GameServerAdmin.Domain.Identity;
using GameServerAdmin.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Auth
{
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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

        // 🔹 회원가입 처리
        [AllowAnonymous]
        [HttpPost("/account/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(GameServerAdmin.Models.Auth.RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Account/Register.cshtml", request);

            // 동일 아이디 존재 여부 체크
            var existing = await _userManager.FindByNameAsync(request.UserName);
            if (existing != null)
            {
                ModelState.AddModelError(string.Empty, "이미 사용 중인 아이디입니다.");
                return View("~/Views/Account/Register.cshtml", request);
            }

            var user = new AppUser
            {
                UserName = request.UserName,
                NickName = request.UserName,       // 추정: 닉네임은 일단 아이디와 동일하게
                CreatedAt = DateTime.UtcNow,
                UserType = "User"                  // 추정: 기본은 일반 유저
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View("~/Views/Account/Register.cshtml", request);
            }

            // 여기서 바로 로그인까지 할지 말지는 선택사항
            // 테스트용이니까, 일단 로그인 없이 로그인 페이지로 보내는 걸로
            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        [HttpGet("/account/denied")]
        public IActionResult Denied() => Content("Access Denied");
    }
}