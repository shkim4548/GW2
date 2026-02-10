using GameServerAdmin.Application.Notice;
using GameServerAdmin.Common.Models;
using GameServerAdmin.Models.Notices.AdminApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Backoffice
{
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    [Route("api/admin/notices")]
    public class NoticeAdminController : ControllerBase
    {
        private readonly IAdminNoticeService _noticeAdminService;

        public NoticeAdminController(AdminNoticeService noticeAdminService)
        {
            _noticeAdminService = noticeAdminService;
        }

        /// <summary>
        /// 공지사항 작성
        /// POST /api/admin/notices
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AdminNoticeResponse>> CreateNotice(
            [FromBody] CreateNoticeRequest request,
            [FromQuery] long adminId)  // 임시: 인증 구현 전까지
        {
            var response = await _noticeAdminService.CreateNoticeAsync(adminId, request);
            return CreatedAtAction(
                nameof(GetNoticeDetail),
                new { noticeId = response.NoticeId },
                response
            );
        }

        /// <summary>
        /// 공지사항 수정
        /// PUT /api/admin/notices/{noticeId}
        /// </summary>
        [HttpPut("{noticeId}")]
        public async Task<ActionResult<AdminNoticeResponse>> UpdateNotice(
            [FromRoute] long noticeId,
            [FromBody] UpdateNoticeRequest request)
        {
            var response = await _noticeAdminService.UpdateNoticeAsync(noticeId, request);
            return Ok(response);
        }

        /// <summary>
        /// 공지사항 상세 조회 (관리자용)
        /// GET /api/admin/notices/{noticeId}
        /// </summary>
        [HttpGet("{noticeId}")]
        public async Task<ActionResult<AdminNoticeResponse>> GetNoticeDetail(
            [FromRoute] long noticeId)
        {
            var response = await _noticeAdminService.GetNoticeDetailAsync(noticeId);
            return Ok(response);
        }

        /// <summary>
        /// 전체 공지사항 목록 조회 (필터링/페이징)
        /// GET /api/admin/notices
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<AdminNoticeListItemDto>>> GetAllNotices(
            [FromQuery] AdminNoticeListQuery query)
        {
            var response = await _noticeAdminService.GetAllNoticesAsync(query);
            return Ok(response);
        }

        /// <summary>
        /// 공지사항 게시 (Draft → Published)
        /// POST /api/admin/notices/{noticeId}/publish
        /// </summary>
        [HttpPost("{noticeId}/publish")]
        public async Task<IActionResult> PublishNotice(
            [FromRoute] long noticeId)
        {
            await _noticeAdminService.PublishNoticeAsync(noticeId);
            return NoContent();
        }

        /// <summary>
        /// 공지사항 숨김 (Published → Hidden)
        /// POST /api/admin/notices/{noticeId}/hide
        /// </summary>
        [HttpPost("{noticeId}/hide")]
        public async Task<IActionResult> HideNotice(
            [FromRoute] long noticeId)
        {
            await _noticeAdminService.HideNoticeAsync(noticeId);
            return NoContent();
        }

        /// <summary>
        /// 공지사항 상단 고정
        /// POST /api/admin/notices/{noticeId}/pin
        /// </summary>
        [HttpPost("{noticeId}/pin")]
        public async Task<IActionResult> PinNotice(
            [FromRoute] long noticeId)
        {
            await _noticeAdminService.PinNoticeAsync(noticeId);
            return NoContent();
        }

        /// <summary>
        /// 공지사항 고정 해제
        /// POST /api/admin/notices/{noticeId}/unpin
        /// </summary>
        [HttpPost("{noticeId}/unpin")]
        public async Task<IActionResult> UnpinNotice(
            [FromRoute] long noticeId)
        {
            await _noticeAdminService.UnpinNoticeAsync(noticeId);
            return NoContent();
        }

        /// <summary>
        /// 댓글 활성화
        /// POST /api/admin/notices/{noticeId}/comments/enable
        /// </summary>
        [HttpPost("{noticeId}/comments/enable")]
        public async Task<IActionResult> EnableComments(
            [FromRoute] long noticeId)
        {
            await _noticeAdminService.EnableCommentsAsync(noticeId);
            return NoContent();
        }

        /// <summary>
        /// 댓글 비활성화
        /// POST /api/admin/notices/{noticeId}/comments/disable
        /// </summary>
        [HttpPost("{noticeId}/comments/disable")]
        public async Task<IActionResult> DisableComments(
            [FromRoute] long noticeId)
        {
            await _noticeAdminService.DisableCommentsAsync(noticeId);
            return NoContent();
        }

        /// <summary>
        /// 공지사항 삭제 (Soft Delete)
        /// DELETE /api/admin/notices/{noticeId}
        /// </summary>
        [HttpDelete("{noticeId}")]
        public async Task<IActionResult> DeleteNotice(
            [FromRoute] long noticeId)
        {
            await _noticeAdminService.DeleteNoticeAsync(noticeId);
            return NoContent();
        }

        /// <summary>
        /// 공지사항 복구
        /// POST /api/admin/notices/{noticeId}/restore
        /// </summary>
        [HttpPost("{noticeId}/restore")]
        public async Task<IActionResult> RestoreNotice(
            [FromRoute] long noticeId)
        {
            await _noticeAdminService.RestoreNoticeAsync(noticeId);
            return NoContent();
        }

        /// <summary>
        /// 공지사항 영구 삭제 (Hard Delete)
        /// DELETE /api/admin/notices/{noticeId}/permanent
        /// </summary>
        [HttpDelete("{noticeId}/permanent")]
        public async Task<IActionResult> HardDeleteNotice(
            [FromRoute] long noticeId)
        {
            await _noticeAdminService.HardDeleteNoticeAsync(noticeId);
            return NoContent();
        }
    }
}
