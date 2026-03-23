using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain.Game.Gacha;

[Table("player_gacha_inventory")]
public class PlayerGachaInventory
{
    protected PlayerGachaInventory() { }

    public PlayerGachaInventory(long userId, long gachaItemId)
    {
        UserId      = userId;
        GachaItemId = gachaItemId;
        AcquiredAt  = DateTime.UtcNow;
    }

    [Key]
    [Column("player_gacha_inventory_id")]
    public long PlayerGachaInventoryId { get; private set; }

    [Column("user_id")]
    public long UserId { get; private set; }

    [Column("gacha_item_id")]
    public long GachaItemId { get; private set; }

    [Column("acquired_at")]
    public DateTime AcquiredAt { get; private set; }

    // Navigation
    public GachaItem? Item { get; private set; }
}
