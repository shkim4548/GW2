using GameServerAdmin.Domain.Comments;

namespace GameServerAdmin.Models.Comments.AdminApi
{
    public class AdminCommentListQuery
    {
        public long? PostId { get; set; }
        public long? AuthorId { get; set; }
        public CommentStatus? Status { get; set; }
        public bool IncludeDeleted { get; set; } = true;

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Sorting
        public string SortBy { get; set; } = "CreatedAt";  // CreatedAt, UpdatedAt, DeletedAt
        public string SortOrder { get; set; } = "desc";    // asc, desc
    }
}