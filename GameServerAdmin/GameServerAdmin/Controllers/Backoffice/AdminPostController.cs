using GameServerAdmin.Application.Posts;
using GameServerAdmin.Models.Posts.AdminApi;
using GameServerAdmin.Models.Posts.AdminUi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.BackOffice;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/posts")]
public class AdminPostController : Controller
{
    private readonly IAdminPostService _postService;

    public AdminPostController(IAdminPostService postService)
    {
        _postService = postService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "댓글 관리";
        return View();
    }

    // READ (Admin)
    [HttpGet("all")]
    public async Task<IActionResult> GetPostsForAdmin()
    {
        return Ok(await _postService.GetAllPostsForAdminAsync());
    }

    // UPDATE
    [HttpPut]
    public async Task<IActionResult> Update(AdminPostUpdateRequest request)
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

    // GET AdminPost/Detail
    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var dto = await _postService.GetPostDetailForAdminAsync(id); // ← 이름/타입 맞추기

        var vm = AdminPostDetailViewModel.FromDto(dto);

        return View(vm); // Views/AdminPost/Detail.cshtml
    }

    // GET /AdminPost/Edit/{id}
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        // 동일하게 상세 DTO 가져와서 EditViewModel로 변환
        var dto = await _postService.GetPostDetailForAdminAsync(id); // ← 추정

        var vm = new AdminPostEditViewModel
        {
            PostId = dto.PostId,
            Title = dto.Title,
            Content = dto.Content
        };

        return View(vm); // Views/AdminPost/Edit.cshtml    }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminPostEditViewModel model)
    {
        if (id != model.PostId)
        {
            // URL과 폼 데이터가 다르면 뭔가 이상한 것이므로 BadRequest 처리
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 추정: Admin 수정용 DTO
        // 레포에 AdminPostUpdateRequest 같은 이름이 있을 가능성이 높다.
        var request = new AdminPostUpdateRequest // ← DTO 이름/필드는 레포에 맞게 수정
        {
            Title = model.Title,
            Content = model.Content,
            // 필요하면 PostType 등도 포함
        };

        // ⚠️ 추정: 수정 서비스 메서드
        await _postService.UpdateAsync(request);

        // 수정 후 상세 화면으로 이동
        return RedirectToAction(nameof(Detail), new { id = model.PostId });
    }
}