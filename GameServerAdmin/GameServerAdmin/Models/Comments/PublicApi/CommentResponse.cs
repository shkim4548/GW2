namespace GameServerAdmin.Models.Comments.PublicApi
{
    public class CommentResponse
    {
        public long Id { get; set; }
        public long PostId { get; set; }
        public long AuthorId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Nested 구조: 대댓글 목록
        public List<ReplyResponse> Replies { get; set; } = new();
    }
}