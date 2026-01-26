using GameServerAdmin.Application.Posts;
using GameServerAdmin.Domain.Posts;
using GameServerAdmin.Models.Posts.AdminApi;
using Microsoft.AspNetCore.Mvc;
using Npgsql.PostgresTypes;

namespace GameServerAdmin.Controllers.BackOffice
{
    [ApiController]
    [Route("api/admin/posts")]
    public class PostAdminController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostAdminController(PostService postService)
        {
            _postService = postService;
        }

        // Create
        [HttpPost]
        public async Task<ActionResult<PostDto>> Create(PostCreateRequest request)
        {
            var post = new Post
            {
                PostType = request.PostType,
                Title = request.Title,
                Content = request.Content,
                AuthorType = request.AuthorType,
                AuthorId = request.AuthorId,
            };

            return Ok(await _postService.CreateAsync(post));
        }

        // READ
        [HttpGet("admin/posts")]
        public async Task<IActionResult> GetPostsForAdmin()
        {
            var posts = await _postService.GetAllPostsForAdminAsync();
            return Ok(posts);
        }

        // UPDATE
        [HttpPut]
        public async Task<IActionResult> Update(PostUpdateRequest request)
        {
            await _postService.UpdateAsync(request);
            return Ok();
        }

        // DELETE (Soft)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _postService.SoftDeleteAsync(id);
            return Ok();
        }

        // Restore
        [HttpPatch("{id:int}/restore")]
        public async Task<IActionResult> Restore(int id)
        {
            await _postService.RestoreAsync(id);
            return Ok();
        }

        // DELETED POSTS LIST GET
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeletedPosted()
        {
            var posts = await _postService.GetDeletedPostAsync();
            return Ok(posts);
        }

        [HttpGet("deleted{id:int}")]
        public async Task<IActionResult> GetDeletedPost(int id)
        {
            var post = await _postService.GetDeletedPostAsync(id);
            return Ok(post);
        }

        // HARD DELETE
        [HttpDelete]
        public async Task<IActionResult> HardDeleted(int id)
        {
            await _postService.HardDeleteAsync(id);
            // 성공했지만 반환할 데이터가 없기 때문에 return No Content
            return NoContent();
        }

        // PAGED QUERY
        [HttpGet]
        public async Task<ActionResult<PagedResponse<AdminPostListItemResponse>>> GetList([FromQuery] AdminPostListQuery query)
        {
            var result = await _postService.GetAdminPostListAsync(query);
            return Ok(result);
        }
    }
}
