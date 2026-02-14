using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Backoffice
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminUiAdminController : Controller
    {
        // GET /AdminUiAdmin/Index
        public IActionResult Index()
        {
            ViewData["Title"] = "Admin Home";
            return View("~/Views/Admin/Index.cshtml");
        }
    }
}
