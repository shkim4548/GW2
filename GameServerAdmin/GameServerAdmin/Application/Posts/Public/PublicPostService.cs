using GameServerAdmin.Common.Exceptions.Post;
using GameServerAdmin.Common.Exceptions.Validation;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Domain.Posts;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Posts.PublicApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
        // Post.AuthorType은 string이므로 .ToString() 비교
        if (post.AuthorId != _userContext.ActorId || post.AuthorType != _userContext.ActorType)
            throw new Common.Exceptions.ForbiddenException("본인이 작성한 게시글만 수정할 수 있습니다.");
    }

    private void EnsureCanDelete(Post post)
    {
        bool isUnknownAuthor = post.AuthorId == 0;

        if (isUnknownAuthor)
        {
            if (_userContext.ActorType != ActorType.ADMIN)
                throw new Common.Exceptions.ForbiddenException("작성자를 알 수 없는 게시글은 관리자만 삭제할 수 있습니다.");
            return;
        }

        if (post.AuthorId != _userContext.ActorId || post.AuthorType != _userContext.ActorType)
            throw new Common.Exceptions.ForbiddenException("본인이 작성한 게시글만 삭제할 수 있습니다.");
    }

    public async Task<PublicPostDetailResponse> CreateAsync(PublicPostCreateRequest request)
    {
        // 1) App Validation
        var fieldErrors = new FieldErrorCollection();

        if (string.IsNullOrWhiteSpace(request.PostType))
            fieldErrors.AddError(nameof(request.PostType), "PostType is required.");
        if (string.IsNullOrWhiteSpace(request.Title))
            fieldErrors.AddError(nameof(request.Title), "Title is required.");
        if (string.IsNullOrWhiteSpace(request.Content))
            fieldErrors.AddError(nameof(request.Content), "Content is required.");

        if (fieldErrors.Any())
            throw new RequestValidationException(fieldErrors);

        // 2) 인증 필수
        EnsureAuthenticated();

        var actorId = _userContext.ActorId;
        var actorType = _userContext.ActorType;

        // 3) ActorType + ActorId 기반으로 AuthorName 조회
        string authorName;

        if (actorType == ActorType.USER) // HttpUserContext.ActorType이 현재 "User" 고정(코드 기준)
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == actorId);

            // 닉네임 없을 때의 fallback 정책은 자유롭게
            authorName = user?.Nickname ?? "(알 수 없음)";
        }
        else if (actorType == ActorType.ADMIN)
        {
            var admin = await _db.Admins
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AdminId == actorId);

            // Admin은 LoginId를 표시 이름으로 사용하는 것으로 추정
            authorName = admin?.LoginId ?? "(관리자)";
        }
        else
        {
            // (불명확) ActorType에 다른 값이 올 가능성은 현재 구조상 낮음
            authorName = "(알 수 없음)";
        }

        // 4) Post 생성 (옵션 A: AuthorName denormalize)
        var post = new Post(
            postType: request.PostType,
            title: request.Title,
            content: request.Content,
            authorType: actorType,
            authorId: actorId,
            authorName: authorName
        );

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        // 5) 방금 생성된 Post를 DTO로 변환해서 반환
        return new PublicPostDetailResponse
        {
            PostId = post.PostId,
            PostType = post.PostType,
            Title = post.Title,
            Content = post.Content,
            AuthorType = post.AuthorType,
            AuthorId = post.AuthorId,
            CreatedAt = post.CreatedAt,
            AuthorName = post.AuthorName,
            ViewCount = post.ViewCount
        };
    }

    public async Task<List<PublicPostDetailResponse>> GetAllAsync()
    {
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
                CreatedAt = p.CreatedAt,
                AuthorName = p.AuthorName,
                ViewCount = p.ViewCount
            })
            .ToListAsync();
    }

    public async Task<PublicPostDetailResponse> GetByIdAsync(int postId)
    {
        // 1️⃣ 엔티티를 Tracking 상태로 가져온다
        var post = await _db.Posts
            .FirstOrDefaultAsync(p => p.PostId == postId && !p.IsDeleted);

        if (post is null)
            throw new PostNotFoundException(postId);

        // 2️⃣ 조회수 증가
        post.IncrementViewCount();

        // 3️⃣ DB 저장
        await _db.SaveChangesAsync();

        // 4️⃣ DTO로 변환
        return new PublicPostDetailResponse
        {
            PostId = post.PostId,
            PostType = post.PostType,
            Title = post.Title,
            Content = post.Content,
            AuthorType = post.AuthorType,
            AuthorId = post.AuthorId,
            CreatedAt = post.CreatedAt,
            ViewCount = post.ViewCount
        };
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

        EnsureCanDelete(post);

        post.SoftDelete();
        await _db.SaveChangesAsync();
    }
}
