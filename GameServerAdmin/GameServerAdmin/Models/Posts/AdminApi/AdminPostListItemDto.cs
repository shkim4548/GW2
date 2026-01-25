namespace GameServerAdmin.Models.Posts.AdminApi
{
    public class AdminPostListItemDto
    {
        public int PostId { get; set; }
        public string PostType { get; set; } = null!;
        public string Title { get; set; } = null!;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
