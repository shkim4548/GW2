using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Backoffice
{
    public class AdminUiServerController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "게임 서버 관리";
            return View("~/Views/AdminServer/Index.cshtml");
        }
    }
}
