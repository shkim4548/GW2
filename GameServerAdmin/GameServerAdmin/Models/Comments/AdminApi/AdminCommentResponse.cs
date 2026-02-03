using GameServerAdmin.Domain.Comments;

namespace GameServerAdmin.Models.Comments.AdminApi
{
    public class AdminCommentResponse
    {
        public long Id { get; set; }
        public long PostId { get; set; }
        public long AuthorId { get; set; }
        public string Content { get; set; } = string.Empty;
        public CommentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Nested 구조: 대댓글 목록 (삭제된 것 포함)
        public List<AdminReplyResponse> Replies { get; set; } = new();
    }
}