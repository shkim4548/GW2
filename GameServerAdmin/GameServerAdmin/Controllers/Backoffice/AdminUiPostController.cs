using GameServerAdmin.Application.Posts;
using GameServerAdmin.Models.Posts.AdminApi;
using GameServerAdmin.Models.Posts.AdminUi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace GameServerAdmin.Controllers.Backoffice
{
    public class AdminUiPostController : Controller
    {
        private readonly IAdminPostService _postService;
        public AdminUiPostController(IAdminPostService postService)
        {
            _postService = postService;
        }

        /// <summary>
        /// Admin 게시판 목록
        /// GET /AdminPost/Index?Page=1&PageSize=20&IsDeleted=true/false/null
        /// (기본 MVC 라우트 사용)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] AdminPostListQuery query)
        {
            // 서비스에서 페이징 결과 조회
            var paged = await _postService.GetAdminPostListAsync(query);

            // 서비스 구현이 Page/PageSize를 안 채우고 있어서, 여기서 맞춰 줌
            paged.Page = query.Page;
            paged.PageSize = query.PageSize;

            var vm = new AdminPostListViewModel
            {
                Query = query,
                PagedResult = paged
            };

            return View(vm);    // Views/AdminPost/Index.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(int id)
        {
            await _postService.SoftDeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            await _postService.RestoreAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete(int id)
        {
            await _postService.HardDeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
