namespace GameServerAdmin.Models.Posts.PublicApi
{
    public class PublicPostListItemResponse
    {
        public long PostId { get; set; }
        public string Title { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; } = null!;
        public int ViewCount { get; set; }
        public string PostType { get; set; } = null!; 
    }
}
