using GameServerAdmin.Common.Exceptions.Post;
using GameServerAdmin.Domain.Posts;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Posts.AdminApi;
using GameServerAdmin.Models.Posts.PublicApi;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace GameServerAdmin.Application.Posts;

public interface IPostService
{
    Task<PostDto> CreateAsync(Post post);
    Task<List<PostDto>> GetAllAsync();
    Task<PostDto?> GetByIdAsync(int postId);
    Task UpdateAsync(PostUpdateRequest request);
    Task SoftDeleteAsync(int postId);
    Task<List<Post>> GetActivePostsAsync();
    Task<List<AdminPostListItemDto>> GetAllPostsForAdminAsync();
    Task RestoreAsync(int postId);
    Task HardDeleteAsync(int postId);
    Task<IReadOnlyList<DeletedPostResponse>> GetDeletedPostAsync();
    Task<DeletedPostDetailResponse> GetDeletedPostAsync(int postId);
    Task<PagedResponse<AdminPostListItemResponse>> GetAdminPostListAsync(AdminPostListQuery query);
    Task<PublicPostDetailResponse> GetPublicPostAsync(int postId);
}

// DTO의 데이터 할당은 Service의 책임범위이므로 Controller에 노출되어서는 안된다.
public class PostService : IPostService
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

        if (post == null)
        {
            throw new InvalidOperationException("수정할 Post가 존재하지 않습니다");
        }

        if(post.IsDeleted)
        {
            throw new InvalidOperationException("삭제된 Post는 수정할 수 없습니다.");
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

        if(post.IsDeleted)
        {
            throw new InvalidOperationException("이미 삭제된 Post입니다.");
        }

        post.IsDeleted = true;
        post.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    // 사용자용 조회 코드
    public async Task<List<Post>> GetActivePostsAsync()
    {
        return await _db.Posts
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    // Admin용 조회 코드
    public async Task<List<AdminPostListItemDto>> GetAllPostsForAdminAsync()
    {
        return await _db.Posts
         .OrderByDescending(p => p.CreatedAt)
         .Select(p => new AdminPostListItemDto
         {
             PostId = p.PostId,
             PostType = p.PostType,
             Title = p.Title,
             IsDeleted = p.IsDeleted,
             CreatedAt = p.CreatedAt,
             UpdatedAt = p.UpdatedAt
         })
         .ToListAsync();
    }

    // Admin용 복원
    public async Task RestoreAsync(int postId)
    {
        var post = await _db.Posts
            .FirstOrDefaultAsync(p => p.PostId == postId);

        if (post == null)
        {
            throw new PostNotFoundException(postId);
        }

        if (!post.IsDeleted)
        {
            throw new InvalidPostStateException("Post is not deleted");
        }

        post.IsDeleted = false;
        post.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    // Admin용 하드 삭제를 위한 목록 조회
    public async Task<IReadOnlyList<DeletedPostResponse>> GetDeletedPostAsync()
    {
        return await _db.Posts
            .Where(p => p.IsDeleted)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new DeletedPostResponse
            {
                PostId = p.PostId,
                PostType = p.PostType,
                Title = p.Title,
                AuthorId = p.AuthorId,
                AuthorType = p.AuthorType,
                UpdatedAt = p.UpdatedAt ?? p.CreatedAt,
            }).ToListAsync();
    }

    // SOFT DELETE 내용 상세 조회
    public async Task<DeletedPostDetailResponse> GetDeletedPostAsync(int postId)
    {
        var post = await _db.Posts
            .FirstOrDefaultAsync(p => p.PostId == postId && p.IsDeleted);

        if (post == null)
        {
            throw new PostNotFoundException(postId);
        }

        return new DeletedPostDetailResponse
        {
            PostId = post.PostId,
            PostType = post.PostType,
            Title = post.Title,
            Content = post.Content,
            AuthorType = post.AuthorType,
            AuthorId = post.AuthorId,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };
    }

    public async Task HardDeleteAsync(int postId)
    {
        var post = await _db.Posts
            .FirstOrDefaultAsync(p => p.PostId == postId);

        if (post == null)
        {
            throw new PostNotFoundException(postId);
        }

        if (!post.IsDeleted)
        {
            throw new InvalidPostStateException("Post must be soft-deleted before hard delete");
        }

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResponse<AdminPostListItemResponse>> GetAdminPostListAsync(AdminPostListQuery query)
    {
        var postsQuery = _db.Posts.AsQueryable();

        if (query.IsDeleted.HasValue)
        {
            postsQuery = postsQuery.Where(p => p.IsDeleted == query.IsDeleted.Value);
        }

        var totalCount = await postsQuery.CountAsync();
        var items = await postsQuery
            .OrderByDescending(p => p.PostId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new AdminPostListItemResponse
            {
                PostId = p.PostId,
                PostType = p.PostType,
                Title = p.Title,
                IsDeleted = p.IsDeleted,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return new PagedResponse<AdminPostListItemResponse>
        {
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<PublicPostDetailResponse> GetPublicPostAsync(int postId)
    {
        var post = await _db.Posts
            .Where(p => !p.IsDeleted && p.PostId == postId)
            .Select(p => new PublicPostDetailResponse
            {
                PostId = p.PostId,
                PostType = p.PostType,
                Title = p.Title,
                Content = p.Content,
                CreatedAt = p.CreatedAt
            }).FirstOrDefaultAsync();

        if (post == null)
            throw new PostNotFoundException(postId);

        return post;
    }
}