using GameServerAdmin.Application.Comments.Public;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Models.Comments.PublicApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Public;

[Authorize]
public sealed class BoardCommentController : Controller
{
    private readonly IPublicCommentService _commentService;
    private readonly IUserContext _userContext;

    public BoardCommentController(IPublicCommentService commentService, IUserContext userContext)
    {
        _commentService = commentService;
        _userContext = userContext;
    }

    // POST /board/{postId}/comments
    [HttpPost("/board/{postId:long}/comments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateComment(long postId, [FromForm] string comment)
    {
        await _commentService.CreateCommentAsync(postId, _userContext.ActorId,
            new CreateCommentRequest { Comment = comment });
        return RedirectToAction("Detail", "Board", new { id = postId });
    }

    // POST /board/{postId}/comments/{commentId}/delete
    [HttpPost("/board/{postId:long}/comments/{commentId:long}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(long postId, long commentId)
    {
        await _commentService.DeleteCommentAsync(postId, commentId, _userContext.ActorId);
        return RedirectToAction("Detail", "Board", new { id = postId });
    }

    // POST /board/{postId}/comments/{parentCommentId}/replies
    [HttpPost("/board/{postId:long}/comments/{parentCommentId:int}/replies")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReply(long postId, int parentCommentId, [FromForm] string content)
    {
        await _commentService.CreateReplyAsync(
            (int)postId, parentCommentId, (int)_userContext.ActorId,
            new CreateReplyRequest { Content = content });
        return RedirectToAction("Detail", "Board", new { id = postId });
    }
}