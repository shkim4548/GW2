using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Backoffice
{
    public class AdminUiLogController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "로그 조회";
            return View("~/Views/AdminLog/Index.cshtml");
        }
    }
}
