using GameServerAdmin.Application.Comments.Public;
using GameServerAdmin.Models.Comments.AdminApi;
using GameServerAdmin.Models.Comments.PublicApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameServerAdmin.Controllers.Public
{
    [ApiController]
    [Route("api/posts/{postId}/comments")]
    public class PublicCommentController : ControllerBase
    {
        private readonly IPublicCommentService _commentService;

        public PublicCommentController(IPublicCommentService commentService)
        {
            _commentService = commentService;
        }

        // 댓글 작성
        // POST /api/posts/{postId}/comments
        // 임시: 인증 구현 전까지 Query로 받음
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CommentResponse>> CreateComment(long postId, [FromBody] CreateCommentRequest request)  
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(userIdValue))
                return Unauthorized();

            var authorId = long.Parse(userIdValue);

            var response = await _commentService.CreateCommentAsync(postId, authorId, request);
            return CreatedAtAction(
                nameof(GetComments),
                new { postId },
                response
            );
        }

        /// <summary>
        /// 대댓글 작성
        /// POST /api/posts/{postId}/comments/{commentId}/replies
        /// </summary>
        [HttpPost("{commentId}/replies")]
        public async Task<ActionResult<ReplyResponse>> CreateReply([FromRoute] int postId, [FromRoute] int commentId, [FromBody] CreateReplyRequest request, [FromQuery] int authorId)
        {
            var response = await _commentService.CreateReplyAsync(postId, commentId, authorId, request);
            return CreatedAtAction(
                nameof(GetComments),
                new { postId },
                response
            );
        }

        /// <summary>
        /// 댓글 수정
        /// PUT /api/posts/{postId}/comments/{commentId}
        /// </summary>
        [HttpPut("{commentId}")]
        public async Task<ActionResult<CommentResponse>> UpdateComment([FromRoute] int postId, [FromRoute] int commentId, [FromBody] UpdateCommentRequest request, [FromQuery] int authorId)
        {
            var response = await _commentService.UpdateCommentAsync(postId, commentId, authorId, request);
            return Ok(response);
        }

        /// <summary>
        /// 댓글 삭제 (Soft Delete)
        /// DELETE /api/posts/{postId}/comments/{commentId}
        /// </summary>
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment([FromRoute] long postId, [FromRoute] long commentId, [FromQuery] long authorId)
        {
            await _commentService.DeleteCommentAsync(postId, commentId, authorId);
            return NoContent();
        }

        /// <summary>
        /// 게시글의 댓글 목록 조회
        /// GET /api/posts/{postId}/comments
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<CommentListResponse>> GetComments([FromRoute] int postId)
        {
            var response = await _commentService.GetCommentsByPostAsync(postId);
            return Ok(response);
        }
    }
}
