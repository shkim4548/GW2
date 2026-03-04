using GameServerAdmin.Common.Exceptions;

namespace GameServerAdmin.Domain.Game.Inventory
{
    public sealed class AdminCurrencyLog
    {
        public long AdminCurrencyLogId { get; private set; }

        public long AdminId { get; private set; }
        public long UserId { get; private set; }
        public CurrencyType CurrencyType { get; private set; }

        /// <summary>양수: 지급, 음수: 회수</summary>
        public long ChangeAmount { get; private set; }

        public long BeforeAmount { get; private set; }
        public long AfterAmount { get; private set; }

        public string Reason { get; private set; } = null!;

        public DateTimeOffset CreatedAt { get; private set; }

        private AdminCurrencyLog() { }

        public AdminCurrencyLog(
            long adminId,
            long userId,
            CurrencyType currencyType,
            long changeAmount,
            long beforeAmount,
            long afterAmount,
            string reason)
        {
            if (changeAmount == 0)
                throw new DomainException("ChangeAmount는 0일 수 없습니다.");

            if (afterAmount < 0)
                throw new DomainException("AfterAmount는 음수가 될 수 없습니다.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new DomainException("Reason은 필수입니다.");

            AdminId = adminId;
            UserId = userId;
            CurrencyType = currencyType;
            ChangeAmount = changeAmount;
            BeforeAmount = beforeAmount;
            AfterAmount = afterAmount;
            Reason = reason;
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}