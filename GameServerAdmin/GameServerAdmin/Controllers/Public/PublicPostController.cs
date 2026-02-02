using GameServerAdmin.Application.Posts.Public;
using GameServerAdmin.Models.Posts.AdminApi;
using GameServerAdmin.Models.Posts.PublicApi;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Public;

[ApiController]
[Route("api/posts")]
public class PublicPostController : ControllerBase
{
    private readonly IPublicPostService _postService;

    public PublicPostController(IPublicPostService postService)
    {
        _postService = postService;
    }

    // CREATE
    [HttpPost]
    public async Task<ActionResult<PublicPostDetailResponse>> Create(PostCreateRequest request)
    {
        return Ok(await _postService.CreateAsync(request));
    }

    // READ ALL
    [HttpGet]
    public async Task<ActionResult<List<PublicPostDetailResponse>>> GetAll()
    {
        return Ok(await _postService.GetAllAsync());
    }

    // READ ONE
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PublicPostDetailResponse>> Get(int id)
    {
        return Ok(await _postService.GetByIdAsync(id));
    }
}