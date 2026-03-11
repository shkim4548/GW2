namespace GameServerAdmin.Models.Admin.AdminUi;

public sealed class AdminAccountListItemViewModel
{
    public long AdminId { get; init; }
    public string LoginId { get; init; } = null!;
    public string Role { get; init; } = null!;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}
