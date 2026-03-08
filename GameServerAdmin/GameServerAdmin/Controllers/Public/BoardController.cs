using GameServerAdmin.Application.Comments.Public;
using GameServerAdmin.Application.Posts.Public;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Models.Posts.PublicApi;
using GameServerAdmin.Models.Posts.PublicUi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Public;

public sealed class BoardController : Controller
{
    private readonly IPublicPostService _postService;
    private readonly IPublicCommentService _commentService;
    private readonly IUserContext _userContext;

    public BoardController(IPublicPostService postService, IPublicCommentService commentService, IUserContext userContext)
    {
        _postService = postService;
        _commentService = commentService;
        _userContext = userContext;
    }

    // GET /board
    [HttpGet("/board")]
    public async Task<IActionResult> Index()
    {
        var posts = await _postService.GetAllAsync();

        var viewModel = posts
            .Select(p => new PublicPostListItemViewModel
            {
                PostId = p.PostId,
                Title = p.Title,
                CreatedAt = p.CreatedAt,
                AuthorName = p.AuthorName,
                ViewCount = p.ViewCount,
            })
            .ToList();

        return View(viewModel);
    }

    // GET /board/{id}
    [HttpGet("/board/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var post = await _postService.GetByIdAsync(id);
        var comments = await _commentService.GetCommentsByPostAsync(id);

        var vm = new PublicBoardDetailViewModel
        {
            Post = new PublicPostDetailViewModel
            {
                PostId = post.PostId,
                Title = post.Title,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                ViewCount = post.ViewCount,
                AuthorName = post.AuthorName,
            },
            Comments = comments,
            IsAuthenticated = _userContext.IsAuthenticated,
            CurrentUserId = _userContext.IsAuthenticated ? _userContext.ActorId : null
        };

        return View(vm);
    }

    // GET /board/create
    [Authorize] // 글쓰기는 로그인 필요 (정책은 네 선택)
    [HttpGet("/board/create")]
    //[ValidateAntiForgeryToken]
    public IActionResult Create()
    {
        var model = new PublicPostCreateViewModel();
        return View(model);
    }

    // POST /board/create
    [Authorize]
    [HttpPost("/board/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PublicPostCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var request = new PublicPostCreateRequest
        {
            // PostType은 지금은 고정값으로 가도 됨 (추정)
            PostType = "PUBLIC",
            Title = model.Title,
            Content = model.Content
        };

        var created = await _postService.CreateAsync(request);
        return RedirectToAction(nameof(Detail), new { id = created.PostId });
    }

    [Authorize]
    [HttpPost]                      // ← /Board/Delete/{id} (컨벤셔널 라우팅)
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _postService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
