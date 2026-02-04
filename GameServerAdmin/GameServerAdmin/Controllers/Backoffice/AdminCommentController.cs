using GameServerAdmin.Application.Comments.Admin;
using GameServerAdmin.Models.Comments.AdminApi;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Backoffice
{
    [ApiController]
    [Route("api/admin/comments")]
    public class CommentAdminController : ControllerBase
    {
        private readonly IAdminCommentService _commentAdminService;

        public CommentAdminController(IAdminCommentService commentAdminService)
        {
            _commentAdminService = commentAdminService;
        }

        // 관리자용 댓글 목록 조회 (삭제된 것 포함)
        // GET /api/admin/comments
        [HttpGet]
        public async Task<ActionResult<PagedResponse<AdminCommentListItemDto>>> GetAllComments([FromQuery] AdminCommentListQuery query)
        {
            var response = await _commentAdminService.GetAllCommentsAsync(query);
            return Ok(response);
        }

        // 관리자용 댓글 상세 조회
        // GET /api/admin/comments/{commentId}
        [HttpGet("{commentId}")]
        public async Task<ActionResult<AdminCommentResponse>> GetCommentDetail([FromRoute] int commentId)
        {
            var response = await _commentAdminService.GetCommentDetailAsync(commentId);
            return Ok(response);
        }

        // 관리자용 댓글 삭제 (Soft Delete)
        // DELETE /api/admin/comments/{commentId}
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment([FromRoute] int commentId)
        {
            await _commentAdminService.DeleteCommentAsync(commentId);
            return NoContent();
        }

        // 댓글 복구
        // POST /api/admin/comments/{commentId}/restore
        [HttpPost("{commentId}/restore")]
        public async Task<IActionResult> RestoreComment([FromRoute] int commentId)
        {
            await _commentAdminService.RestoreCommentAsync(commentId);
            return NoContent();
        }

        // 댓글 영구 삭제 (Hard Delete)
        // DELETE /api/admin/comments/{commentId}/permanent
        [HttpDelete("{commentId}/permanent")]
        public async Task<IActionResult> HardDeleteComment([FromRoute] int commentId)
        {
            await _commentAdminService.HardDeleteCommentAsync(commentId);
            return NoContent();
        }
    }
}

