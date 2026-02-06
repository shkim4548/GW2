using GameServerAdmin.Domain.Notices;

namespace GameServerAdmin.Models.Notices.AdminApi
{
    public class AdminNoticeResponse
    {
        public long NoticeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public long AdminId { get; set; }
        public NoticeCategory Category { get; set; }
        public NoticePriority Priority { get; set; }
        public NoticeStatus Status { get; set; }
        public bool IsPinned { get; set; }
        public DateTime? DisplayStartAt { get; set; }
        public DateTime? DisplayEndAt { get; set; }
        public int ViewCount { get; set; }
        public bool IsCommentEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
