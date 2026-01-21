using GameServerAdmin.Application.Posts;
using GameServerAdmin.Domain.Posts;
using GameServerAdmin.Models.Posts;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers;

[ApiController]
[Route("api/posts")]
public class PostController : ControllerBase
{
    private readonly PostService _postService;

    public PostController(PostService postService)
    {
        _postService = postService;
    }

    [HttpPost]
    public async Task<ActionResult<PostDto>> Create(PostCreateRequest request)
    {
        var post = new Post
        {
            PostType = request.PostType,
            Title = request.Title,
            Content = request.Content,
            AuthorType = request.AuthorType,
            AuthorId = request.AuthorId
        };

        var result = await _postService.CreateAsync(post);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<PostDto>>> GetAll()
    {
        return Ok(await _postService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PostDto>> Get(int id)
    {
        var post = await _postService.GetByIdAsync(id);
        if (post == null)
            return NotFound();

        return Ok(post);
    }
}
