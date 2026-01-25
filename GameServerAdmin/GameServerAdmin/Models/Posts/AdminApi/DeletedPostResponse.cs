namespace GameServerAdmin.Models.Posts.AdminApi
{
    public class DeletedPostResponse
    {
        public int PostId { get; set; }
        public string PostType { get; set; } = null!;
        public string Title { get; set; } = null!;
        public int AuthorId { get; set; }
        public string AuthorType { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
    }
}
