using GameServerAdmin.Application.Posts;
using GameServerAdmin.Models.Posts.AdminApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.BackOffice;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/posts")]
public class AdminPostController : ControllerBase
{
    private readonly IAdminPostService _postService;

    public AdminPostController(IAdminPostService postService)
    {
        _postService = postService;
    }

    // PAGED LIST (기본 GET 하나만 남김)
    // GET /api/admin/posts?page=1&pageSize=20&isDeleted=true/false
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] AdminPostListQuery query)
    {
        return Ok(await _postService.GetAdminPostListAsync(query));
    }

    // 전체 리스트(필요하면 유지, 아니면 제거 가능)
    // GET /api/admin/posts/all
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _postService.GetAllPostsForAdminAsync());
    }

    // UPDATE
    // PUT /api/admin/posts
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] PostUpdateRequest request)
    {
        await _postService.UpdateAsync(request);
        return NoContent();
    }

    // SOFT DELETE
    // DELETE /api/admin/posts/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete([FromRoute] int id)
    {
        await _postService.SoftDeleteAsync(id);
        return NoContent();
    }

    // RESTORE
    // PATCH /api/admin/posts/{id}/restore
    [HttpPatch("{id:int}/restore")]
    public async Task<IActionResult> Restore([FromRoute] int id)
    {
        await _postService.RestoreAsync(id);
        return NoContent();
    }

    // DELETED LIST
    // GET /api/admin/posts/deleted
    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeletedPosts()
    {
        return Ok(await _postService.GetDeletedPostAsync());
    }

    // DELETED DETAIL
    // GET /api/admin/posts/deleted/{id}
    [HttpGet("deleted/{id:int}")]
    public async Task<IActionResult> GetDeletedPost([FromRoute] int id)
    {
        return Ok(await _postService.GetDeletedPostAsync(id));
    }

    // HARD DELETE
    // DELETE /api/admin/posts/{id}/hard
    [HttpDelete("{id:int}/hard")]
    public async Task<IActionResult> HardDelete([FromRoute] int id)
    {
        await _postService.HardDeleteAsync(id);
        return NoContent();
    }
}