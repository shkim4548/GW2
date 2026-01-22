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
        private readonly PostService _postService;

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
    }
}
