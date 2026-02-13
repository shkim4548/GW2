using GameServerAdmin.Common.Models;
using GameServerAdmin.Models.Posts.AdminApi;

namespace GameServerAdmin.Models.Posts.AdminUi
{
    public sealed class AdminPostListViewModel
    {
        public AdminPostListQuery Query { get; init; } = new();
        public PagedResponse<AdminPostListItemResponse> PagedResult { get; init; } = new();
    }
}
