using GameServerAdmin.Common.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain.Game.Gacha;

[Table("player_gacha_currency")]
public class PlayerGachaCurrency
{
    protected PlayerGachaCurrency() { }

    public PlayerGachaCurrency(long userId)
    {
        UserId = userId;
        Amount = 0;
    }

    [Key]
    [Column("user_id")]
    public long UserId { get; private set; }

    [Column("amount")]
    public int Amount { get; private set; }

    public void Add(int amount)
    {
        if (amount <= 0)
            throw new DomainException("지급 금액은 0보다 커야 합니다.");
        Amount += amount;
    }

    public void Subtract(int cost)
    {
        if (cost <= 0)
            throw new DomainException("차감 금액은 0보다 커야 합니다.");
        if (Amount < cost)
            throw new DomainException("가챠 재화가 부족합니다.");
        Amount -= cost;
    }
}
