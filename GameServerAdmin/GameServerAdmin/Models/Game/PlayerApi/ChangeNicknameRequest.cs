using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Game.PlayerApi
{
    /// <summary>
    /// 플레이어 닉네임 변경 요청 DTO
    /// </summary>
    public class ChangeNicknameRequest
    {
        /// <summary>새 닉네임</summary>
        [Required]
        [StringLength(20, MinimumLength = 1)]
        public string NewNickname { get; set; } = null!;
    }
}