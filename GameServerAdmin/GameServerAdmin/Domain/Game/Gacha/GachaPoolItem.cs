using GameServerAdmin.Common.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain.Game.Gacha;

[Table("gacha_pool_item")]
public class GachaPoolItem
{
    protected GachaPoolItem() { }

    public GachaPoolItem(long gachaPoolId, long gachaItemId, int weight)
    {
        if (weight <= 0)
            throw new DomainException("가중치는 0보다 커야 합니다.");

        GachaPoolId  = gachaPoolId;
        GachaItemId  = gachaItemId;
        Weight       = weight;
    }

    [Key]
    [Column("gacha_pool_item_id")]
    public long GachaPoolItemId { get; private set; }

    [Column("gacha_pool_id")]
    public long GachaPoolId { get; private set; }

    [Column("gacha_item_id")]
    public long GachaItemId { get; private set; }

    [Column("weight")]
    public int Weight { get; private set; }

    // Navigation
    public GachaPool? Pool { get; private set; }
    public GachaItem? Item { get; private set; }
}
