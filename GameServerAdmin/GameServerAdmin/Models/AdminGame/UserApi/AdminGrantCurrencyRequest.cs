using GameServerAdmin.Domain.Game.Inventory;
using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.AdminGame.UserApi
{
    public class AdminGrantCurrencyRequest
    {
        [Required]
        public CurrencyType CurrencyType { get; set; }

        /// <summary>양수: 지급, 음수: 회수</summary>
        [Required]
        public long ChangeAmount { get; set; }

        [Required]
        [MaxLength(200)]
        public string Reason { get; set; } = null!;
    }
}