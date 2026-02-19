using GameServerAdmin.Application.Posts.Public;
using GameServerAdmin.Models.Posts.PublicApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Public;

[ApiController]
[Route("api/posts")]
public sealed class PublicPostController : ControllerBase
{
    private readonly IPublicPostService _postService;

    public PublicPostController(IPublicPostService postService)
    {
        _postService = postService;
    }

    // CREATE
    [Authorize /* (추정) JWT만 강제하려면 AuthenticationSchemes를 지정 */]
    // [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost]
    public async Task<ActionResult<PublicPostDetailResponse>> Create([FromBody] PublicPostCreateRequest request)
    {
        var created = await _postService.CreateAsync(request);

        // REST스럽게: 생성된 리소스 위치 포함
        return CreatedAtAction(nameof(Get), new { id = created.PostId }, created);
    }

    // LIST
    [HttpGet]
    public async Task<ActionResult<List<PublicPostDetailResponse>>> GetAll()
    {
        return Ok(await _postService.GetAllAsync());
    }

    // DETAIL
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PublicPostDetailResponse>> Get(int id)
    {
        return Ok(await _postService.GetByIdAsync(id));
    }

    // UPDATE
    [Authorize /* JWT 강제 여부는 위와 동일 */]
    // [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PublicPostDetailResponse>> Update(int id, [FromBody] PublicPostUpdateRequest request)
    {
        return Ok(await _postService.UpdateAsync(id, request));
    }

    // DELETE
    [Authorize /* JWT 강제 여부는 위와 동일 */]
    // [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _postService.DeleteAsync(id);
        return NoContent();
    }
}
