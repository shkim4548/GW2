using GameServerAdmin.Domain.Notices;

namespace GameServerAdmin.Models.Notices.AdminApi
{
    public class AdminNoticeListQuery
    {
        public NoticeCategory? Category { get; set; }
        public NoticePriority? Priority { get; set; }
        public NoticeStatus? Status { get; set; }
        public bool IncludeDeleted { get; set; } = true;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string SortBy { get; set; } = "CreatedAt";
        public string SortOrder { get; set; } = "desc";
    }
}