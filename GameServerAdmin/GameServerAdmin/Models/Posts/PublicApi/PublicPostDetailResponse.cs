namespace GameServerAdmin.Models.Posts.PublicApi
{
    public class PublicPostDetailResponse
    {
        public long PostId { get; set; }
        public string PostType { get; set; } = null!; 
        public string Title { get; set; } = null!; 
        public string Content { get; set; } = null!; 
        public string AuthorType { get; set; } = null!; 
        public long AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
