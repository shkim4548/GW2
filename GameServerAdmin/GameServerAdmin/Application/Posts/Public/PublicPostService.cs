using GameServerAdmin.Common.Exceptions.Post;
using GameServerAdmin.Common.Exceptions.Validation;
using GameServerAdmin.Domain.Posts;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Posts.AdminApi;
using GameServerAdmin.Models.Posts.PublicApi;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Posts.Public
{
    public interface IPublicPostService
    {
        /*-------------------
            Public Service
         --------------------*/

        Task<PublicPostDetailResponse> CreateAsync(PostCreateRequest request);
        Task<List<PublicPostDetailResponse>> GetAllAsync();
        Task<PublicPostDetailResponse?> GetByIdAsync(int postId);
        Task<PublicPostDetailResponse> GetPublicPostAsync(int postId);

    }

    public class PublicPostService : IPublicPostService
    {
        private readonly AppDbContext _db;

        public PublicPostService(AppDbContext db)
        {
            _db = db;
        }

        private static PublicPostDetailResponse ToDto(Post post)
        {
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

        public async Task<PublicPostDetailResponse> CreateAsync(PostCreateRequest request)
        {
            // 1. Application Validation
            var fieldErrors = new FieldErrorCollection();

            if (string.IsNullOrWhiteSpace(request.Title))
                fieldErrors.AddError(nameof(request.Title), "Title is required");

            if (fieldErrors.Any())
                throw new RequestValidationException(fieldErrors);

            // 2. Domain 생성 (핵심 변경)
            var post = new Post(
                request.PostType,
                request.Title,
                request.Content,
                request.AuthorType,
                request.AuthorId
            );

            // 3. Persist
            _db.Posts.Add(post);
            await _db.SaveChangesAsync();

            return ToDto(post);
        }

        public async Task<List<PublicPostDetailResponse>> GetAllAsync()
        {
            return await _db.Posts
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.PostId)
                .Select(p => ToDto(p))
                .ToListAsync();
        }

        public async Task<PublicPostDetailResponse?> GetByIdAsync(int postId)
        {
            var post = await _db.Posts
                .FirstOrDefaultAsync(p => p.PostId == postId && !p.IsDeleted);

            if (post == null)
                throw new PostNotFoundException(postId);

            return ToDto(post);
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
}
