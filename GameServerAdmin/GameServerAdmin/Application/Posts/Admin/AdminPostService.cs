using GameServerAdmin.Common.Exceptions.Post;
using GameServerAdmin.Common.Exceptions.Validation;
using GameServerAdmin.Common.Models;
using GameServerAdmin.Domain.Posts;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Posts.AdminApi;
using GameServerAdmin.Models.Posts.PublicApi;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;

namespace GameServerAdmin.Application.Posts;

public interface IAdminPostService
{
    /*--------------------
        Admin Service
     ---------------------*/
    Task UpdateAsync(AdminPostUpdateRequest request);
    Task SoftDeleteAsync(int postId);
    Task<List<Post>> GetActivePostsAsync();
    Task<List<AdminPostListItemDto>> GetAllPostsForAdminAsync();
    Task RestoreAsync(int postId);
    Task HardDeleteAsync(int postId);
    Task<IReadOnlyList<DeletedPostResponse>> GetDeletedPostAsync();
    Task<DeletedPostDetailResponse> GetDeletedPostAsync(int postId);
    Task<PagedResponse<AdminPostListItemResponse>> GetAdminPostListAsync(AdminPostListQuery query);
    Task<AdminPostDetailResponse> GetPostDetailForAdminAsync(int postId);
}

// DTO의 데이터 할당은 Service의 책임범위이므로 Controller에 노출되어서는 안된다.
public class AdminPostService : IAdminPostService
{
    private readonly AppDbContext _db;

    public AdminPostService(AppDbContext db)
    {
        _db = db;
    }


    public async Task UpdateAsync(AdminPostUpdateRequest request)
    {
        var post = await _db.Posts
            .FirstOrDefaultAsync(p =>
            p.PostId == request.PostId && !p.IsDeleted);

        if (post == null)
        {
            throw new PostNotFoundException(request.PostId);
        }

        if (post.IsDeleted)
        {
            throw new InvalidPostStateException("삭제된 Post는 수정할 수 없습니다.");
        }

        // 도메인 수정
        post.Update(request.Title, request.Content);

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
            throw new PostNotFoundException(postId);
        }

        if (post.IsDeleted)
        {
            throw new InvalidPostStateException("이미 삭제된 Post입니다.");
        }

        post.SoftDelete();

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

        post.Restore();

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

    // Admin
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

    // Admin
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

    public async Task<AdminPostDetailResponse> GetPostDetailForAdminAsync(int postId)
    {
        var post = await _db.Posts
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.PostId == postId);

        if (post == null) throw new PostNotFoundException(postId);

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