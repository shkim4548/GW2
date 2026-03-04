using GameServerAdmin.Common.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain.Game.Inventory
{
    /// <summary>
    /// 유저별 재화 보유량
    /// </summary>
    [Table("player_currency")]
    public class PlayerCurrency
    {
        protected PlayerCurrency() { }

        public PlayerCurrency(long userId, CurrencyType currencyType, long amount)
        {
            if (userId <= 0)
                throw new DomainException("유효하지 않은 UserId 입니다.");

            if (amount < 0)
                throw new DomainException("재화 수량은 음수가 될 수 없습니다.");

            UserId = userId;
            CurrencyType = currencyType;
            Amount = amount;
        }

        [Key]
        [Column("player_currency_id")]
        public long PlayerCurrencyId { get; private set; }

        /// <summary>게임 유저(User.UserId)</summary>
        [Column("user_id")]
        public long UserId { get; private set; }

        /// <summary>재화 타입</summary>
        [Column("currency_type")]
        public CurrencyType CurrencyType { get; private set; }

        /// <summary>보유량</summary>
        [Column("amount")]
        public long Amount { get; private set; }

        /// <summary>
        /// 낙관적 동시성 제어용 RowVersion
        /// </summary>
        [Timestamp] // System.ComponentModel.DataAnnotations
        public byte[] RowVersion { get; private set; } = default!;

        public void Add(long value)
        {
            if (value < 0)
                throw new DomainException("재화 증가는 음수일 수 없습니다.");

            checked
            {
                Amount += value;
            }
        }

        public void Subtract(long value)
        {
            if (value < 0)
                throw new DomainException("재화 차감은 음수일 수 없습니다.");

            if (Amount < value)
                throw new DomainException($"재화가 부족합니다. (현재:{Amount}, 필요:{value})");

            Amount -= value;
        }
    }
}