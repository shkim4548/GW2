using GameServerAdmin.Common.Security;

namespace GameServerAdmin.Models.Posts.AdminApi
{
    public sealed class AdminPostDetailResponse
    {
        public long PostId { get; set; }
        public string PostType { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public ActorType AuthorType { get; set; }
        public long AuthorId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

}
