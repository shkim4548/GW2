using GameServerAdmin.Domain.Users;
using System;

namespace GameServerAdmin.Models.Game.PlayerApi
{
    /// <summary>
    /// 플레이어(게임 유저) 프로필 응답 DTO
    /// </summary>
    public class PlayerProfileResponse
    {
        /// <summary>게임 유저(User) PK</summary>
        public long UserId { get; set; }

        /// <summary>닉네임</summary>
        public string Nickname { get; set; } = null!;

        /// <summary>플레이어 레벨</summary>
        public long Level { get; set; }

        /// <summary>계정 생성 시각(UTC 기준)</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>마지막 로그인 시각(UTC 기준)</summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>계정 상태 (예: Active, Banned 등)</summary>
        public UserStatus Status { get; set; }
    }
}