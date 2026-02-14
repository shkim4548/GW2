using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.BackOffice;

[Authorize(Policy = "AdminOnly")]
public class AdminUiNoticeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "공지 관리";
        return View("~/Views/AdminNotice/Index.cshtml");
    }
}