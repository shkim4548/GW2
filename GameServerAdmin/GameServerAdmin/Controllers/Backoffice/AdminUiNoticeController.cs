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

        // 기존 진입점이 Index라면 List로 유도
        public IActionResult Index() => RedirectToAction(nameof(List));

        // GET /AdminUiNotice/List
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] AdminNoticeListQuery query)
        {
            ViewData["Title"] = "공지 관리";
            var response = await _noticeService.GetAllNoticesAsync(query);
            return View("~/Views/AdminNotice/Index.cshtml", response);
        }

        // GET /AdminUiNotice/Detail?noticeId=1
        [HttpGet]
        public async Task<IActionResult> Detail(long noticeId)
        {
            ViewData["Title"] = "공지 상세";
            var notice = await _noticeService.GetNoticeDetailAsync(noticeId);
            return View("~/Views/AdminNotice/Detail.cshtml", notice);
        }

        // GET /AdminUiNotice/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "공지 작성";
            return View("~/Views/AdminNotice/Create.cshtml", new CreateNoticeRequest());
        }

        // POST /AdminUiNotice/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateNoticeRequest model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/AdminNotice/Create.cshtml", model);

            var adminId = GetAdminIdOrThrow();
            await _noticeService.CreateNoticeAsync(adminId, model);
            return RedirectToAction(nameof(List));
        }

        // GET /AdminUiNotice/Edit?noticeId=1
        [HttpGet]
        public async Task<IActionResult> Edit(long noticeId)
        {
            ViewData["Title"] = "공지 수정";

            var detail = await _noticeService.GetNoticeDetailAsync(noticeId);

            // DTO 필드명이 다르면 여기만 맞춰서 수정하면 됨
            var vm = new UpdateNoticeRequest
            {
                Title = detail.Title,
                Content = detail.Content,
                Category = detail.Category,
                Priority = detail.Priority,
                DisplayStartAt = detail.DisplayStartAt,
                DisplayEndAt = detail.DisplayEndAt
            };

            ViewData["NoticeId"] = noticeId;
            return View("~/Views/AdminNotice/Edit.cshtml", vm);
        }

        // POST /AdminUiNotice/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long noticeId, UpdateNoticeRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["NoticeId"] = noticeId;
                return View("~/Views/AdminNotice/Edit.cshtml", model);
            }

            await _noticeService.UpdateNoticeAsync(noticeId, model);
            return RedirectToAction(nameof(Detail), new { noticeId });
        }

        // ---- 상태/토글 액션 (Detail 화면에서 POST) ----

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(long noticeId)
        {
            await _noticeService.PublishNoticeAsync(noticeId);
            return RedirectToAction(nameof(Detail), new { noticeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(long noticeId)
        {
            await _noticeService.HideNoticeAsync(noticeId);
            return RedirectToAction(nameof(Detail), new { noticeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pin(long noticeId)
        {
            await _noticeService.PinNoticeAsync(noticeId);
            return RedirectToAction(nameof(Detail), new { noticeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpin(long noticeId)
        {
            await _noticeService.UnpinNoticeAsync(noticeId);
            return RedirectToAction(nameof(Detail), new { noticeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableComments(long noticeId)
        {
            await _noticeService.EnableCommentsAsync(noticeId);
            return RedirectToAction(nameof(Detail), new { noticeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableComments(long noticeId)
        {
            await _noticeService.DisableCommentsAsync(noticeId);
            return RedirectToAction(nameof(Detail), new { noticeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long noticeId)
        {
            await _noticeService.DeleteNoticeAsync(noticeId);
            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(long noticeId)
        {
            await _noticeService.RestoreNoticeAsync(noticeId);
            return RedirectToAction(nameof(Detail), new { noticeId });
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