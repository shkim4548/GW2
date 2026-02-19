using GameServerAdmin.Common.Exceptions.Post;
using GameServerAdmin.Common.Exceptions.Validation;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Domain.Posts;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Posts.PublicApi;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Posts.Public;

public interface IPublicPostService
{
    Task<PublicPostDetailResponse> CreateAsync(PublicPostCreateRequest request);
    Task<List<PublicPostDetailResponse>> GetAllAsync();
    Task<PublicPostDetailResponse> GetByIdAsync(int postId);
    Task<PublicPostDetailResponse> UpdateAsync(int postId, PublicPostUpdateRequest request);
    Task DeleteAsync(int postId);
}

public sealed class PublicPostService : IPublicPostService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _userContext;

    public PublicPostService(AppDbContext db, IUserContext userContext)
    {
        _db = db;
        _userContext = userContext;
    }

    private void EnsureAuthenticated()
    {
        if (!_userContext.IsAuthenticated)
            throw new UnauthorizedAccessException("Authentication required.");
    }

    private void EnsureOwner(Post post)
    {
        // (추정) 더 안전한 정책: AuthorId + AuthorType 모두 비교
        // (불명확) AuthorType이 항상 "User"로 저장되는지, Admin도 작성 가능한지에 따라 정책 변경 가능
        if (post.AuthorId != _userContext.ActorId || post.AuthorType != _userContext.ActorType)
            throw new UnauthorizedAccessException("You are not the owner of this post.");
    }

    public async Task<PublicPostDetailResponse> CreateAsync(PublicPostCreateRequest request)
    {
        // 1) App Validation (도메인 정책만 여기서 추가, 기본 Required/Length는 ModelState로 커버 가능(추정))
        var fieldErrors = new FieldErrorCollection();

        if (string.IsNullOrWhiteSpace(request.PostType))
            fieldErrors.AddError(nameof(request.PostType), "PostType is required.");
        if (string.IsNullOrWhiteSpace(request.Title))
            fieldErrors.AddError(nameof(request.Title), "Title is required.");
        if (string.IsNullOrWhiteSpace(request.Content))
            fieldErrors.AddError(nameof(request.Content), "Content is required.");

        if (fieldErrors.Any())
            throw new RequestValidationException(fieldErrors);

        EnsureAuthenticated();

        var post = new Post(
            postType: request.PostType,
            title: request.Title,
            content: request.Content,
            authorType: _userContext.ActorType,
            authorId: _userContext.ActorId
        );

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        // 저장 후에는 엔티티로 DTO 구성(여긴 DB 번역 이슈 없음)
        return new PublicPostDetailResponse
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

    public async Task<List<PublicPostDetailResponse>> GetAllAsync()
    {
        // ✅ EF 번역 안전: Select 내부에서 new DTO 직접 생성
        return await _db.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.PostId)
            .Select(p => new PublicPostDetailResponse
            {
                PostId = p.PostId,
                PostType = p.PostType,
                Title = p.Title,
                Content = p.Content,
                AuthorType = p.AuthorType,
                AuthorId = p.AuthorId,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<PublicPostDetailResponse> GetByIdAsync(int postId)
    {
        // ✅ EF 번역 안전 + 불필요한 엔티티 트래킹 제거
        var dto = await _db.Posts
            .AsNoTracking()
            .Where(p => p.PostId == postId && !p.IsDeleted)
            .Select(p => new PublicPostDetailResponse
            {
                PostId = p.PostId,
                PostType = p.PostType,
                Title = p.Title,
                Content = p.Content,
                AuthorType = p.AuthorType,
                AuthorId = p.AuthorId,
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (dto is null)
            throw new PostNotFoundException(postId);

        return dto;
    }

    public async Task<PublicPostDetailResponse> UpdateAsync(int postId, PublicPostUpdateRequest request)
    {
        var fieldErrors = new FieldErrorCollection();

        if (string.IsNullOrWhiteSpace(request.Title))
            fieldErrors.AddError(nameof(request.Title), "Title is required.");
        if (string.IsNullOrWhiteSpace(request.Content))
            fieldErrors.AddError(nameof(request.Content), "Content is required.");

        if (fieldErrors.Any())
            throw new RequestValidationException(fieldErrors);

        EnsureAuthenticated();

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == postId && !p.IsDeleted);
        if (post is null)
            throw new PostNotFoundException(postId);

        EnsureOwner(post);

        post.Update(request.Title, request.Content);
        await _db.SaveChangesAsync();

        return new PublicPostDetailResponse
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

    public async Task DeleteAsync(int postId)
    {
        EnsureAuthenticated();

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == postId && !p.IsDeleted);
        if (post is null)
            throw new PostNotFoundException(postId);

        EnsureOwner(post);

        post.SoftDelete();
        await _db.SaveChangesAsync();
    }
}
