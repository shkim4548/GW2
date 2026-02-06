using GameServerAdmin.Domain.Notices;

namespace GameServerAdmin.Models.Notices.PublicApi
{
    public class NoticeResponse
    {
        public long NoticeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public NoticeCategory Category { get; set; }
        public NoticePriority Priority { get; set; }
        public bool IsPinned { get; set; }
        public int ViewCount { get; set; }
        public bool IsCommentEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
