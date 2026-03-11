using GameServerAdmin.Application.Notice;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Public
{
    public class NoticeController : Controller
    {
        private readonly IPublicNoticeService _noticeService;

        public NoticeController(IPublicNoticeService noticeService)
        {
            _noticeService = noticeService;
        }

        // GET /notice
        [HttpGet("/notice")]
        public async Task<IActionResult> Index()
        {
            var response = await _noticeService.GetNoticeAsync();
            return View(response);
        }

        // GET /notice/{noticeId}
        [HttpGet("/notice/{noticeId:long}")]
        public async Task<IActionResult> Detail(long noticeId)
        {
            var notice = await _noticeService.GetNoticeDetailAsync(noticeId);
            return View(notice);
        }
    }
}
