using GameServerAdmin.Application.Notice;
using GameServerAdmin.Domain.Notices;
using GameServerAdmin.Models.Notices.PublicApi;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Public
{
    [ApiController]
    [Route("api/notices")]
    public class PublicNoticeController : ControllerBase
    {
        private readonly IPublicNoticeService _noticeService;

        public PublicNoticeController(PublicNoticeService publicNoticeService)
        {
            _noticeService = publicNoticeService;
        }

        // 공지사항 목록 조회
        [HttpGet]
        public async Task<ActionResult<NoticeListResponse>> GetNotices()
        {
            var response = await _noticeService.GetNoticeAsync();
            return Ok(response);
        }

        [HttpGet("{noticeId}")]
        public async Task<ActionResult<NoticeResponse>> GetNoticeDetail([FromRoute] long noticeId)
        {
            var response = await _noticeService.GetNoticeDetailAsync(noticeId);
            return Ok(response);
        }

        [HttpGet("pinned")]
        public async Task<ActionResult<List<NoticeResponse>>> GetPinnedNotices()
        {
            var response = await _noticeService.GetPinnedNoticeAsync();
            return Ok(response);
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<NoticeListResponse>> GetNoticeByCategory([FromRoute] NoticeCategory category)
        {
            var response = await _noticeService.GetNoticeByCategoryAsync(category);
            return Ok(response);
        }
    }
}
