using System.Security.Claims;
using GameServerAdmin.Application.Notice;
using GameServerAdmin.Models.Notices.AdminApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Backoffice
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminUiNoticeController : Controller
    {
        private readonly IAdminNoticeService _noticeService;

        public AdminUiNoticeController(IAdminNoticeService noticeService)
        {
            _noticeService = noticeService;
        }

        [HttpGet("/admin/ui/notice")]
        public IActionResult Index() => RedirectToAction(nameof(List));

        [HttpGet("/admin/ui/notice/list")]
        public async Task<IActionResult> List([FromQuery] AdminNoticeListQuery query)
        {
            ViewData["Title"] = "공지 관리";
            var response = await _noticeService.GetAllNoticesAsync(query);
            return View("~/Views/AdminNotice/Index.cshtml", response);
        }

        [HttpGet("/admin/ui/notice/detail/{noticeId}")]
        public async Task<IActionResult> Detail(long noticeId)
        {
            ViewData["Title"] = "공지 상세";
            var notice = await _noticeService.GetNoticeDetailAsync(noticeId);
            return View("~/Views/AdminNotice/Detail.cshtml", notice);
        }

        [HttpGet("/admin/ui/notice/create")]
        public IActionResult Create()
        {
            ViewData["Title"] = "공지 작성";
            return View("~/Views/AdminNotice/Create.cshtml", new CreateNoticeRequest());
        }

        [HttpPost("/admin/ui/notice/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateNoticeRequest model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/AdminNotice/Create.cshtml", model);

            var adminId = GetAdminIdOrThrow();
            await _noticeService.CreateNoticeAsync(adminId, model);
            return RedirectToAction(nameof(List));
        }

        // Edit = 공지 상세 + 액션 버튼 페이지
        [HttpGet("/admin/ui/notice/edit")]
        public async Task<IActionResult> Edit([FromQuery] long noticeId)
        {
            ViewData["Title"] = "공지 관리";
            var detail = await _noticeService.GetNoticeDetailAsync(noticeId);
            return View("~/Views/AdminNotice/Edit.cshtml", detail);
        }

        [HttpPost("/admin/ui/notice/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromQuery] long noticeId, UpdateNoticeRequest model)
        {
            if (!ModelState.IsValid)
            {
                var detail = await _noticeService.GetNoticeDetailAsync(noticeId);
                return View("~/Views/AdminNotice/Edit.cshtml", detail);
            }
            await _noticeService.UpdateNoticeAsync(noticeId, model);
            return RedirectToAction(nameof(Edit), new { noticeId });
        }

        [HttpPost("/admin/ui/notice/publish")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(long noticeId)
        {
            await _noticeService.PublishNoticeAsync(noticeId);
            return RedirectToAction(nameof(Edit), new { noticeId });
        }

        [HttpPost("/admin/ui/notice/hide")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(long noticeId)
        {
            await _noticeService.HideNoticeAsync(noticeId);
            return RedirectToAction(nameof(Edit), new { noticeId });
        }

        [HttpPost("/admin/ui/notice/pin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pin(long noticeId)
        {
            await _noticeService.PinNoticeAsync(noticeId);
            return RedirectToAction(nameof(Edit), new { noticeId });
        }

        [HttpPost("/admin/ui/notice/unpin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpin(long noticeId)
        {
            await _noticeService.UnpinNoticeAsync(noticeId);
            return RedirectToAction(nameof(Edit), new { noticeId });
        }

        [HttpPost("/admin/ui/notice/enable-comments")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableComments(long noticeId)
        {
            await _noticeService.EnableCommentsAsync(noticeId);
            return RedirectToAction(nameof(Edit), new { noticeId });
        }

        [HttpPost("/admin/ui/notice/disable-comments")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableComments(long noticeId)
        {
            await _noticeService.DisableCommentsAsync(noticeId);
            return RedirectToAction(nameof(Edit), new { noticeId });
        }

        [HttpPost("/admin/ui/notice/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long noticeId)
        {
            await _noticeService.DeleteNoticeAsync(noticeId);
            return RedirectToAction(nameof(List));
        }

        [HttpPost("/admin/ui/notice/restore")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(long noticeId)
        {
            await _noticeService.RestoreNoticeAsync(noticeId);
            return RedirectToAction(nameof(Edit), new { noticeId });
        }

        private long GetAdminIdOrThrow()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (long.TryParse(raw, out var id))
                return id;
            throw new InvalidOperationException("AdminId claim not found or invalid.");
        }
    }
}
