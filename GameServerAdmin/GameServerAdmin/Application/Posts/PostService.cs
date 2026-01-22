using GameServerAdmin.Domain.Posts;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Posts;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Posts;

// DTO의 데이터 할당은 Service의 책임범위이므로 Controller에 노출되어서는 안된다.
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

    public async Task UpdateAsync(PostUpdateRequest request)
    {
        var post = await _db.Posts
            .FirstOrDefaultAsync(p =>
            p.PostId == request.PostId && !p.IsDeleted);

        if(post == null)
        {
            throw new InvalidOperationException("수정할 Post가 존재하지 않습니다");
        }

        // 도메인 수정
        post.Title = request.Title;
        post.Content = request.Content;
        post.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int postId)
    {
        var post = await _db.Posts
            .FirstOrDefaultAsync(p =>
                p.PostId == postId &&
                !p.IsDeleted);

        if (post == null)
        {
            throw new InvalidOperationException("삭제할 Post가 존재하지 않습니다.");
        }

        post.IsDeleted = true;
        post.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

}
