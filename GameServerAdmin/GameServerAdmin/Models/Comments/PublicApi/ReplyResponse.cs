namespace GameServerAdmin.Models.Comments.PublicApi
{
    public class ReplyResponse
    {
        public long Id { get; set; }
        public long ParentCommentId { get; set; }
        public long AuthorId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}