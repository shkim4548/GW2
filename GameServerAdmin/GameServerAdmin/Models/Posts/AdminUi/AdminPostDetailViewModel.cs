using System.ComponentModel.DataAnnotations;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Models.Posts.AdminApi;

namespace GameServerAdmin.Models.Posts.AdminUi
{
    public sealed class AdminPostDetailViewModel
    {
        public long PostId { get; init; }
        public string PostType { get; init; } = null!;
        public string Title { get; init; } = null!;
        public string Content { get; init; } = null!;
        public ActorType AuthorType { get; init; }
        public long AuthorId { get; init; }
        public bool IsDeleted { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? DeletedAt { get; init; }  // 삭제 시각이 있다면

        // ⚠️ 여기서 AdminPostDetailResponse는 레포에 있는 DTO 이름을 추정한다.
        // 실제 이름/필드는 레포에 맞게 수정 필요.
        public static AdminPostDetailViewModel FromDto(AdminPostDetailResponse dto) // ← DTO 이름은 추정
        {
            return new AdminPostDetailViewModel
            {
                PostId = dto.PostId,
                PostType = dto.PostType,
                Title = dto.Title,
                Content = dto.Content,
                AuthorType = dto.AuthorType,
                AuthorId = dto.AuthorId,
                IsDeleted = dto.IsDeleted,
                CreatedAt = dto.CreatedAt,
                DeletedAt = dto.DeletedAt
            };
        }
    }
}
