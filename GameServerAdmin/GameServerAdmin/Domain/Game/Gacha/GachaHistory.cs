using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain.Game.Gacha;

[Table("gacha_history")]
public class GachaHistory
{
    protected GachaHistory() { }

    public GachaHistory(long userId, long gachaPoolId, long gachaItemId,
                        PullType pullType, bool isDuplicate, int compensationAmount)
    {
        UserId              = userId;
        GachaPoolId         = gachaPoolId;
        GachaItemId         = gachaItemId;
        PullType            = pullType;
        IsDuplicate         = isDuplicate;
        CompensationAmount  = compensationAmount;
        PulledAt            = DateTime.UtcNow;
    }

    [Key]
    [Column("gacha_history_id")]
    public long GachaHistoryId { get; private set; }

    [Column("user_id")]
    public long UserId { get; private set; }

    [Column("gacha_pool_id")]
    public long GachaPoolId { get; private set; }

    [Column("gacha_item_id")]
    public long GachaItemId { get; private set; }

    [Column("pull_type")]
    public PullType PullType { get; private set; }

    [Column("is_duplicate")]
    public bool IsDuplicate { get; private set; }

    [Column("compensation_amount")]
    public int CompensationAmount { get; private set; }

    [Column("pulled_at")]
    public DateTime PulledAt { get; private set; }
}
