using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Backoffice
{
    public class AdminUiUserController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "유저 관리";
            return View("~/Views/AdminUser/Index.cshtml");
        }
    }
}
