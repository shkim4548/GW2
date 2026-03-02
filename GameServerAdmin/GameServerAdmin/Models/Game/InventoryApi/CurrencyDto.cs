using GameServerAdmin.Domain.Game.Inventory;

namespace GameServerAdmin.Models.Game.InventoryApi
{
    public class CurrencyDto
    {
        /// <summary>CurrencyType enum 값</summary>
        public CurrencyType CurrencyType { get; set; }

        /// <summary>보유량</summary>
        public long Amount { get; set; }
    }
}