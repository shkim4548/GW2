namespace GameServerAdmin.Models.Posts.AdminApi
{
    public class DeletedPostDetailResponse
    {
        public int PostId { get; set; }
        public string PostType { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string AuthorType { get; set; } = null!;
        public int AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
