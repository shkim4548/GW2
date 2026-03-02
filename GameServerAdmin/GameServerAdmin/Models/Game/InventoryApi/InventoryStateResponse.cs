using System.Collections.Generic;

namespace GameServerAdmin.Models.Game.InventoryApi
{
    public class InventoryStateResponse
    {
        /// <summary>현재 유저의 모든 재화 목록</summary>
        public List<CurrencyDto> Currencies { get; set; } = new();
    }
}