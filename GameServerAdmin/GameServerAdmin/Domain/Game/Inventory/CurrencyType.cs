namespace GameServerAdmin.Domain.Game.Inventory
{
    /// <summary>
    /// 게임 내 기본 재화 타입
    /// </summary>
    public enum CurrencyType
    {
        /// <summary>스태미너(행동력)</summary>
        Stamina = 1,

        /// <summary>골드(게임 내 머니)</summary>
        Gold = 2,

        /// <summary>젬(유료/프리미엄 재화)</summary>
        Gem = 3
    }
}