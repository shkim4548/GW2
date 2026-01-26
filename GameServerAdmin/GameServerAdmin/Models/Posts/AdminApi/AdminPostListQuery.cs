namespace GameServerAdmin.Models.Posts.AdminApi
{
    public class AdminPostListQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool? IsDeleted { get; set; }    // null : 전체, true : 삭제, false : 삭제안된 것 전체
    }
}
