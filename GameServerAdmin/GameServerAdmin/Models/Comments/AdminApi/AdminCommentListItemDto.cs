using GameServerAdmin.Domain.Comments;

namespace GameServerAdmin.Models.Comments.AdminApi
{
    public class AdminCommentListItemDto
    {
        public long Id { get; set; }
        public long PostId { get; set; }
        public long? ParentCommentId { get; set; }
        public long AuthorId { get; set; }
        public string Content { get; set; } = string.Empty;
        public CommentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int ReplyCount { get; set; }  // 대댓글 수
    }
}