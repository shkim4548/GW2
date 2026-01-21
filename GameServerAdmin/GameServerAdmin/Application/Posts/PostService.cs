using GameServerAdmin.Domain.Posts;
using GameServerAdmin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Posts;

public class PostService
{
    private readonly AppDbContext _db;

    public PostService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PostDto> CreateAsync(Post post)
    {
        post.CreatedAt = DateTime.UtcNow;
        post.IsDeleted = false;

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        return ToDto(post);
    }

    public async Task<List<PostDto>> GetAllAsync()
    {
        return await _db.Posts
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.PostId)
            .Select(p => ToDto(p))
            .ToListAsync();
    }

    public async Task<PostDto?> GetByIdAsync(int postId)
    {
        var post = await _db.Posts
            .FirstOrDefaultAsync(p => p.PostId == postId && !p.IsDeleted);

        return post == null ? null : ToDto(post);
    }

    private static PostDto ToDto(Post post)
    {
        return new PostDto
        {
            PostId = post.PostId,
            PostType = post.PostType,
            Title = post.Title,
            Content = post.Content,
            AuthorType = post.AuthorType,
            AuthorId = post.AuthorId,
            CreatedAt = post.CreatedAt
        };
    }
}
