namespace GameServerAdmin.Models.Posts.PublicApi
{
    // 이건 조회용
    public class PublicPostSearchRequest
    {
        public string? PostType { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
