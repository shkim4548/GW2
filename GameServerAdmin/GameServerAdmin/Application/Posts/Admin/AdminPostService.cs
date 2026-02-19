using GameServerAdmin.Common.Exceptions.Post;
using GameServerAdmin.Common.Exceptions.Validation;
using GameServerAdmin.Common.Models;
using GameServerAdmin.Domain.Posts;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Posts.AdminApi;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Posts;

public interface IAdminPostService
{
    Task UpdateAsync(AdminPostUpdateRequest request);
    Task SoftDeleteAsync(int postId);
    Task RestoreAsync(int postId);
    Task HardDeleteAsync(int postId);

    Task<PagedResponse<AdminPostListItemResponse>> GetAdminPostListAsync(AdminPostListQuery query);
    Task<AdminPostDetailResponse> GetPostDetailForAdminAsync(int postId);
}

/// <summary>
/// Admin 전용 Post 서비스.
/// - AdminApi DTO만 사용.
/// - Domain(Post)을 외부로 반환하지 않음.
/// </summary>
public sealed class AdminPostService : IAdminPostService
{
    private const int MaxPageSize = 200; // (추정) 운영툴 상한
    private readonly AppDbContext _db;

    public AdminPostService(AppDbContext db)
    {
        _db = db;
    }

    public async Task UpdateAsync(AdminPostUpdateRequest request)
    {
        // Application Validation (DataAnnotation은 ModelState에서 잡히지만 방어적으로 보강)
        var errors = new FieldErrorCollection();

        if (request.PostId <= 0)
            errors.AddError(nameof(request.PostId), "PostId must be greater than 0.");
        if (string.IsNullOrWhiteSpace(request.Title))
            errors.AddError(nameof(request.Title), "Title is required.");
        if (string.IsNullOrWhiteSpace(request.Content))
            errors.AddError(nameof(request.Content), "Content is required.");

        if (errors.Any())
            throw new RequestValidationException(errors);

        var post = await _db.Posts
            .FirstOrDefaultAsync(p => p.PostId == request.PostId);

        if (post is null)
            throw new PostNotFoundException(request.PostId);

        if (post.IsDeleted)
            throw new InvalidPostStateException("삭제된 Post는 수정할 수 없습니다.");

        // Domain 메서드가 존재한다는 전제 (네 코드에 이미 있음)
        post.Update(request.Title, request.Content);

        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int postId)
    {
        if (postId <= 0)
            throw new RequestValidationException(new FieldErrorCollection
            {
                { "postId", new List<string> { "postId must be greater than 0." } }
            });

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == postId);

        if (post is null)
            throw new PostNotFoundException(postId);

        if (post.IsDeleted)
            throw new InvalidPostStateException("이미 삭제된 Post입니다.");

        post.SoftDelete();
        await _db.SaveChangesAsync();
    }

    public async Task RestoreAsync(int postId)
    {
        if (postId <= 0)
            throw new RequestValidationException(new FieldErrorCollection
            {
                { "postId", new List<string> { "postId must be greater than 0." } }
            });

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == postId);

        if (post is null)
            throw new PostNotFoundException(postId);

        if (!post.IsDeleted)
            throw new InvalidPostStateException("Post is not deleted.");

        post.Restore();
        await _db.SaveChangesAsync();
    }

    public async Task HardDeleteAsync(int postId)
    {
        if (postId <= 0)
            throw new RequestValidationException(new FieldErrorCollection
            {
                { "postId", new List<string> { "postId must be greater than 0." } }
            });

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == postId);

        if (post is null)
            throw new PostNotFoundException(postId);

        if (!post.IsDeleted)
            throw new InvalidPostStateException("Post must be soft-deleted before hard delete.");

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResponse<AdminPostListItemResponse>> GetAdminPostListAsync(AdminPostListQuery query)
    {
        // Query Validation
        var errors = new FieldErrorCollection();

        if (query.Page <= 0)
            errors.AddError(nameof(query.Page), "Page must be greater than 0.");

        if (query.PageSize <= 0)
            errors.AddError(nameof(query.PageSize), "PageSize must be greater than 0.");

        if (query.PageSize > MaxPageSize)
            errors.AddError(nameof(query.PageSize), $"PageSize must be <= {MaxPageSize}.");

        if (errors.Any())
            throw new RequestValidationException(errors);

        var postsQuery = _db.Posts.AsNoTracking().AsQueryable();

        if (query.IsDeleted.HasValue)
            postsQuery = postsQuery.Where(p => p.IsDeleted == query.IsDeleted.Value);

        // (추정) PostType, Title 검색 등의 필터가 query에 추가될 수 있음. 있으면 여기서 확장.

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

    public async Task<AdminPostDetailResponse> GetPostDetailForAdminAsync(int postId)
    {
        if (postId <= 0)
            throw new RequestValidationException(new FieldErrorCollection
            {
               { "postId", new List<string> { "postId must be greater than 0." } }
            });

        var post = await _db.Posts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PostId == postId);

        if (post is null)
            throw new PostNotFoundException(postId);

        return new AdminPostDetailResponse
        {
            PostId = post.PostId,
            PostType = post.PostType,
            Title = post.Title,
            Content = post.Content,
            AuthorType = post.AuthorType,
            AuthorId = post.AuthorId,
            IsDeleted = post.IsDeleted,
            CreatedAt = post.CreatedAt,
            DeletedAt = post.DeletedAt
        };
    }
}
