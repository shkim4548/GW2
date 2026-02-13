using GameServerAdmin.Models.Posts.PublicApi;
using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Posts.PublicUi
{
    // 리스트 화면에서 쓸 최소 정보
    public sealed class PublicPostListItemViewModel
    {
        public long PostId { get; set; }
        public string Title { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string PostType { get; init; } = null!; // 공지/자유 등 나중에 써먹기 좋음

    }

    public sealed class PublicPostDetailViewModel
    {
        public long PostId { get; init; }
        public string PostType { get; init; } = null!;
        public string Title { get; init; } = null!;
        public string Content { get; init; } = null!;
        public string AuthorType { get; init; } = null!;
        public long AuthorId { get; init; }
        public DateTime CreatedAt { get; init; }

        public static PublicPostDetailViewModel FromDto(PublicPostDetailResponse dto)
        {
            return new PublicPostDetailViewModel
            {
                PostId = dto.PostId,
                PostType = dto.PostType,
                Title = dto.Title,
                Content = dto.Content,
                AuthorType = dto.AuthorType,
                AuthorId = dto.AuthorId,
                CreatedAt = dto.CreatedAt
            };
        }
    }
    public sealed class PublicPostCreateViewModel
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;
    }
}
