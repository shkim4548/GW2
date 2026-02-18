using GameServerAdmin.Domain.Identity;
using GameServerAdmin.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        public async Task<IActionResult> Login(LoginRequest request, string? returnUrl = null)
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

        [AllowAnonymous]
        [HttpGet("/account/denied")]
        public IActionResult Denied() => Content("Access Denied");
    }
}