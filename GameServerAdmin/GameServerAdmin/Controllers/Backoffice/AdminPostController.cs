using GameServerAdmin.Application.Posts;
using GameServerAdmin.Common.Exceptions.Validation;
using GameServerAdmin.Models.Posts.AdminApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.BackOffice;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/posts")]
public sealed class AdminPostController : ControllerBase
{
    private readonly IAdminPostService _postService;

    public AdminPostController(IAdminPostService postService)
    {
        _postService = postService;
    }

    // LIST (페이징/검색은 이후. 지금은 query 구조만 유지)
    // GET /api/admin/posts?page=1&pageSize=20&isDeleted=true/false
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] AdminPostListQuery query)
    {
        return Ok(await _postService.GetAdminPostListAsync(query));
    }

    // DETAIL (deleted 포함 상세)
    // GET /api/admin/posts/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail([FromRoute] int id)
    {
        return Ok(await _postService.GetPostDetailForAdminAsync(id));
    }

    // UPDATE
    // PUT /api/admin/posts/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] AdminPostUpdateRequest request)
    {
        // Route id와 body id 불일치 방지 (클라이언트 실수 방어)
        if (request.PostId != id)
        {
            var errors = new FieldErrorCollection();
            errors.AddError(nameof(request.PostId), "Body PostId must match route id.");
            throw new RequestValidationException(errors);
        }

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

    // HARD DELETE
    // DELETE /api/admin/posts/{id}/hard
    [HttpDelete("{id:int}/hard")]
    public async Task<IActionResult> HardDelete([FromRoute] int id)
    {
        await _postService.HardDeleteAsync(id);
        return NoContent();
    }
}
