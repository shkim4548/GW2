using GameServerAdmin.Common.Security;

namespace GameServerAdmin.Models.Posts.PublicApi
{
    public class PublicPostDetailResponse
    {
        public long PostId { get; set; }
        public string PostType { get; set; } = null!; 
        public string Title { get; set; } = null!; 
        public string Content { get; set; } = null!; 
        public ActorType AuthorType { get; set; } 
        public long AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }

        public string AuthorName { get; set; } = null!;
        public int ViewCount { get; set; }
    }
}
