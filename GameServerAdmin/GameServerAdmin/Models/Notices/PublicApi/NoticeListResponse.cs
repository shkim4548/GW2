namespace GameServerAdmin.Models.Notices.PublicApi
{
    public class NoticeListResponse
    {
        public int TotalCount { get; set; }
        public int PinnedCount { get; set; }
        public List<NoticeResponse> Notices { get; set; } = new();
    }
}