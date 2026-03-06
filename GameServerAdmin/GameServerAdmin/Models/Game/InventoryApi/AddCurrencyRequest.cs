using System.ComponentModel.DataAnnotations;
using GameServerAdmin.Domain.Game.Inventory;

namespace GameServerAdmin.Models.Game.InventoryApi
{
    public class AddCurrencyRequest
    {
        public CurrencyType CurrencyType { get; set; }

        [Range(1, long.MaxValue)]
        public long Amount { get; set; }
    }
}