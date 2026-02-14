using GameServerAdmin.Application.Comments.Admin;
using GameServerAdmin.Models.Comments.AdminApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.BackOffice;

[Authorize(Policy = "AdminOnly")]
public class AdminUiCommentController : Controller
{
    private readonly IAdminCommentService _commentService;

    public AdminUiCommentController(IAdminCommentService commentService)
    {
        _commentService = commentService;
    }
    
    // GET /AdminUiComment/Index
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AdminCommentListQuery query)
    {
        ViewData["Title"] = "댓글 관리";
        var response = await _commentService.GetAllCommentsAsync(query);
        return View("~/Views/AdminComment/Index.cshtml", response);
    }

    // DELETE
    [HttpPost]
    public async Task<IActionResult> Delete(int commentId)
    {
        await _commentService.DeleteCommentAsync(commentId);
        return RedirectToAction(nameof(Index));
    }

    // UPDATE
    [HttpPost]
    public async Task<IActionResult> Restore(int commentId)
    {
        await _commentService.RestoreCommentAsync(commentId);
        return RedirectToAction(nameof(Index));
    }
}