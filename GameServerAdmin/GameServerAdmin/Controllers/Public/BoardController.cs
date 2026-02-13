using GameServerAdmin.Application.Posts.Public;
using GameServerAdmin.Models.Posts.AdminApi;
using GameServerAdmin.Models.Posts.PublicUi;
using Microsoft.AspNetCore.Mvc;
using static GameServerAdmin.Models.Posts.PublicUi.PublicPostDetailViewModel;

namespace GameServerAdmin.Controllers.Public
{
    public class BoardController : Controller
    {
        private readonly IPublicPostService _postService;
        
        public BoardController(IPublicPostService postService)
        {
            _postService = postService;
        }

        // 리스트 뷰 확보
        // GET
        [HttpGet("/board")]
        public async Task<IActionResult> Index()
        {
            // 서비스에서 DTO 가져오기
            var posts = await _postService.GetAllAsync();

            // UI ViewModel로 매핑
            var viewModel = posts
                .Select(p => new PublicPostListItemViewModel
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    CreatedAt = p.CreatedAt,
                }).ToList();

            // Razor Viwe로 전달
            return View(viewModel);
        }

        // 상세 View
        [HttpGet("/board/{id:long}")]
        public async Task<IActionResult> Detail(long id)
        {
            // 단일 게시글 DTO 조회
            var post = await _postService.GetByIdAsync((int)id);

            // UI View Model로 매핑
            var viewModel = new PublicPostDetailViewModel
            {
                PostId = post.PostId,
                Title = post.Title,
                Content = post.Content,
                CreatedAt = post.CreatedAt
            };

            // Razor View로 전달
            return View(viewModel);
        }

        // GET : Create
        [HttpGet("/board/create")]
        public IActionResult Create()
        {
            return View(new PublicPostCreateViewModel());
        }

        // POST : Create
        [HttpPost("/board/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PublicPostCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // 클라이언트 측 DataAnnotations 검증 실패
                return View(model);
            }

            // AdminApi.PostCreateRequest로 매핑(서비스 시그니처에 맞추기)
            var request = new PostCreateRequest
            {
                // 여기 값들은 Public 게시판 기본값으로 추정한다.
                PostType = "PUBLIC",       // TODO: 필요하면 상수/enum으로 분리
                Title = model.Title,
                Content = model.Content,
                AuthorType = "GUEST",      // 또는 "USER" 등, 도메인 정책에 맞게 결정
                AuthorId = 0               // 로그인 유저가 아직 없다면 0/1 같은 기본값
            };

            // 서비스 호출
            var created = await _postService.CreateAsync(request);
            return RedirectToAction(nameof(Detail), new { id = (int)created.PostId });
        }
    }
}
