namespace GameServerAdmin.Models.AdminGame.UserApi
{
    public class UserCurrencySummaryDto
    {
        /// <summary>CurrencyType enum 값 (Stamina=1, Gold=2, Gem=3)</summary>
        public int CurrencyType { get; set; }

        /// <summary>보유량</summary>
        public long Amount { get; set; }
    }
}