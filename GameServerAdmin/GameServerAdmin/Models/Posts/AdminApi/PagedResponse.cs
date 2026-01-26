namespace GameServerAdmin.Models.Posts.AdminApi
{
    public class PagedResponse<T>
    {
        public int TotalCount { get; set; }
        public IReadOnlyList<T> Items { get; set; } = [];
    }
}
