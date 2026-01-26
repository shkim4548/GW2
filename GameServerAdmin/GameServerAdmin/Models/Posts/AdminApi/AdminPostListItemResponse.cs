namespace GameServerAdmin.Models.Posts.AdminApi
{
    public class AdminPostListItemResponse
    {
        public int PostId { get; set; }
        public string PostType { get; set; } = null!;
        public string Title { get; set; } = null!;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
