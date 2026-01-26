namespace GameServerAdmin.Models.Posts.PublicApi
{
    public class PublicPostDetailResponse
    {
        public int PostId { get; set; }
        public string PostType { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Content {  get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
