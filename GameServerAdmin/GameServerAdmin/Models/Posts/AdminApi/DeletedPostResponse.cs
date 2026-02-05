namespace GameServerAdmin.Models.Posts.AdminApi
{
    public class DeletedPostResponse
    {
        public long PostId { get; set; }
        public string PostType { get; set; } = null!;
        public string Title { get; set; } = null!;
        public long AuthorId { get; set; }
        public string AuthorType { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
    }
}
