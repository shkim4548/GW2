using GameServerAdmin.Domain.Notices;

namespace GameServerAdmin.Models.Notices.AdminApi
{
    public class AdminNoticeListItemDto
    {
        public long NoticeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public long AdminId { get; set; }
        public NoticeCategory Category { get; set; }
        public NoticePriority Priority { get; set; }
        public NoticeStatus Status { get; set; }
        public bool IsPinned { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}