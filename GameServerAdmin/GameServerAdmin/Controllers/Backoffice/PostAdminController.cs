using GameServerAdmin.Application.Posts;
using GameServerAdmin.Models.Posts.AdminApi;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.BackOffice;

[ApiController]
[Route("api/admin/posts")]
public class PostAdminController : ControllerBase
{
    private readonly IAdminPostService _postService;

    public PostAdminController(IAdminPostService postService)
    {
        _postService = postService;
    }

    // READ (Admin)
    [HttpGet("all")]
    public async Task<IActionResult> GetPostsForAdmin()
    {
        return Ok(await _postService.GetAllPostsForAdminAsync());
    }

    // UPDATE
    [HttpPut]
    public async Task<IActionResult> Update(PostUpdateRequest request)
    {
        await _postService.UpdateAsync(request);
        return Ok();
    }

    // SOFT DELETE
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        await _postService.SoftDeleteAsync(id);
        return Ok();
    }

    // RESTORE
    [HttpPatch("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        await _postService.RestoreAsync(id);
        return Ok();
    }

    // DELETED LIST
    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeletedPosts()
    {
        return Ok(await _postService.GetDeletedPostAsync());
    }

    // DELETED DETAIL
    [HttpGet("deleted/{id:int}")]
    public async Task<IActionResult> GetDeletedPost(int id)
    {
        return Ok(await _postService.GetDeletedPostAsync(id));
    }

    // HARD DELETE
    [HttpDelete("{id:int}/hard")]
    public async Task<IActionResult> HardDelete(int id)
    {
        await _postService.HardDeleteAsync(id);
        return NoContent();
    }

    // PAGED LIST
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] AdminPostListQuery query)
    {
        return Ok(await _postService.GetAdminPostListAsync(query));
    }
}