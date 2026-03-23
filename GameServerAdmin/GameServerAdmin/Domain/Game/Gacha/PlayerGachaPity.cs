using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain.Game.Gacha;

[Table("player_gacha_pity")]
public class PlayerGachaPity
{
    protected PlayerGachaPity() { }

    public PlayerGachaPity(long userId, long gachaPoolId)
    {
        UserId      = userId;
        GachaPoolId = gachaPoolId;
        PullCount   = 0;
    }

    [Key]
    [Column("player_gacha_pity_id")]
    public long PlayerGachaPityId { get; private set; }

    [Column("user_id")]
    public long UserId { get; private set; }

    [Column("gacha_pool_id")]
    public long GachaPoolId { get; private set; }

    [Column("pull_count")]
    public int PullCount { get; private set; }

    public void Increment(int count = 1) => PullCount += count;
    public void Reset()                  => PullCount  = 0;
}
