using GameServerAdmin.Application.Posts;
using GameServerAdmin.Models.Posts.AdminApi;
using GameServerAdmin.Models.Posts.AdminUi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.BackOffice;

// View 반환 전용 컨트롤러 (ApiController/Route("api/...") 절대 금지)
[Authorize(Policy = "AdminOnly")]
public class AdminUiPostController : Controller
{
    private readonly IAdminPostService _postService;

    public AdminUiPostController(IAdminPostService postService)
    {
        _postService = postService;
    }

    // GET /admin/Index  (기본 라우트)
    // 또는 [HttpGet("/admin/posts")]로 바꿔도 됨
    [HttpGet("/admin/ui/posts")]
    public async Task<IActionResult> Index([FromQuery] AdminPostListQuery query)
    {
        var paged = await _postService.GetAdminPostListAsync(query);

        // 서비스가 page/pageSize를 안 채우는 경우를 대비한 보정(필요하면 유지)
        paged.Page = query.Page;
        paged.PageSize = query.PageSize;

        var vm = new AdminPostListViewModel
        {
            Query = query,
            PagedResult = paged
        };

        ViewData["Title"] = "게시글 관리";
        return View("~/Views/AdminPost/Index.cshtml", vm);
    }

    // GET /AdminUiPost/Detail/{id}
    [HttpGet("/admin/ui/posts/{id}")]
    public async Task<IActionResult> Detail(int id)
    {
        var dto = await _postService.GetPostDetailForAdminAsync(id);
        var vm = AdminPostDetailViewModel.FromDto(dto);

        ViewData["Title"] = "게시글 상세";
        return View("~/Views/AdminPost/Detail.cshtml", vm);
    }

    // GET /AdminUiPost/Edit/{id}
    [HttpGet("/admin/ui/posts/{id}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _postService.GetPostDetailForAdminAsync(id);

        var vm = new AdminPostEditViewModel
        {
            PostId = dto.PostId,
            Title = dto.Title,
            Content = dto.Content
        };

        ViewData["Title"] = "게시글 수정";
        return View("~/Views/AdminPost/Edit.cshtml", vm);
    }

    // POST /AdminUiPost/Edit/{id}
    [HttpPost("admin/ui/posts/{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminPostEditViewModel model)
    {
        if (id != model.PostId) 
            return BadRequest();
        if (!ModelState.IsValid)
            return View("~/Views/AdminPost/Edit.cshtml", model);

        // 레포에 실제 존재하는 DTO: PostUpdateRequest 사용
        var request = new AdminPostUpdateRequest
        {
            // ⚠️ PostUpdateRequest 필드 구성에 맞춰 조정
            // 만약 PostId가 DTO에 없다면, 서비스 시그니처가 UpdateAsync(int postId, PostUpdateRequest req)여야 함
            PostId = id,
            Title = model.Title,
            Content = model.Content
        };

        // 네가 "예외 없이 구현 완료"라 했으니,
        // 현재 IAdminPostService.UpdateAsync(PostUpdateRequest)를 유지한다면
        // request에 PostId가 포함되어 있어야 함.
        // 그렇지 않다면 아래 호출을 UpdateAsync(id, request)로 바꿔야 함.
        await _postService.UpdateAsync(request);

        return RedirectToAction(nameof(Detail), new { id = model.PostId });
    }

    // POST /AdminUiPost/SoftDelete/{id}
    [HttpPost("/admin/ui/posts/{id}/soft-delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoftDelete(int id)
    {
        await _postService.SoftDeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/ui/posts/{id}/restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        await _postService.RestoreAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/ui/posts/{id}/hard-delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HardDelete(int id)
    {
        await _postService.HardDeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}