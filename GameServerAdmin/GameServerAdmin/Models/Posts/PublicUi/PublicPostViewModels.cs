using GameServerAdmin.Models.Posts.PublicApi;
using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Posts.PublicUi
{
    // 리스트 화면에서 쓸 최소 정보
    public sealed class PublicPostListItemViewModel
    {
        public long PostId { get; init; }
        public string Title { get; init; } = null!;
        public DateTime CreatedAt { get; init; }

        public string AuthorName { get; init; } = null!;   // 작성자 이름 (UI 표시에 사용)
        public int ViewCount { get; init; }                // 조회수

        public string PostType { get; init; } = null!;     // 공지/자유 등

        public static PublicPostListItemViewModel FromDto(PublicPostListItemResponse dto)
        {
            return new PublicPostListItemViewModel
            {
                PostId = dto.PostId,
                Title = dto.Title,
                CreatedAt = dto.CreatedAt,
                AuthorName = dto.AuthorName,   // ← dto에 이 필드가 있다는 가정(추정)
                ViewCount = dto.ViewCount,     // ← dto에 이 필드가 있다는 가정(추정)
                PostType = dto.PostType
            };
        }
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
        public string AuthorName { get; init; } = null!;
        public long ViewCount { get; init; }

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
                CreatedAt = dto.CreatedAt,
                AuthorName = dto.AuthorName,
                ViewCount = dto.ViewCount,
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
