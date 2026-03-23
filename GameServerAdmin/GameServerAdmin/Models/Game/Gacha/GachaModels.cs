using GameServerAdmin.Domain.Game.Gacha;

namespace GameServerAdmin.Models.Game.Gacha;

// ─── 요청 ────────────────────────────────────────────────────────────────────

public record GachaPullRequest(long UserId);

public record AdminGrantCurrencyRequest(long UserId, int Amount);

public record AdminCreatePoolRequest(
    string Name,
    int SinglePullCost,
    int TenPullCost,
    int PityThreshold,
    GachaGrade PityGrade,
    DateTime StartAt,
    DateTime EndAt);

public record AdminCreateItemRequest(
    string Name,
    GachaGrade Grade,
    int DuplicateCompensation);

public record AdminAddPoolItemRequest(long GachaItemId, int Weight);

// ─── 응답 ────────────────────────────────────────────────────────────────────

public record GachaPullResultItem(
    long GachaItemId,
    string Name,
    GachaGrade Grade,
    bool IsDuplicate,
    int CompensationAmount);

public record GachaPullResponse(
    List<GachaPullResultItem> Results,
    int RemainingCurrency);

public record GachaCurrencyResponse(long UserId, int Amount);

public record GachaHistoryItem(
    long GachaHistoryId,
    long GachaItemId,
    string ItemName,
    GachaGrade Grade,
    PullType PullType,
    bool IsDuplicate,
    int CompensationAmount,
    DateTime PulledAt);

public record GachaPoolResponse(
    long GachaPoolId,
    string Name,
    int SinglePullCost,
    int TenPullCost,
    int PityThreshold,
    GachaGrade PityGrade,
    bool IsActive,
    DateTime StartAt,
    DateTime EndAt);
