using GameServerAdmin.Domain.Game.Inventory;

namespace GameServerAdmin.Models.AdminGame.UserApi
{
    public class AdminGrantCurrencyResponse
    {
        public long UserId { get; set; }
        public CurrencyType CurrencyType { get; set; }
        public long BeforeAmount { get; set; }
        public long AfterAmount { get; set; }
        public long ChangeAmount { get; set; }
    }
}