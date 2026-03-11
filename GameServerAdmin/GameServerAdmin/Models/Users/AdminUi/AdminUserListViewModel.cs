using GameServerAdmin.Models.AdminGame.UserApi;

namespace GameServerAdmin.Models.Users.AdminUi;

public sealed class AdminUserListViewModel
{
    public string? Nickname { get; init; }
    public long? UserId { get; init; }
    public bool HasSearched { get; init; }
    public List<AdminUserSearchItemResponse> Results { get; init; } = new();

    // 페이징 : 전체 목록일 때만 사용한다
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
