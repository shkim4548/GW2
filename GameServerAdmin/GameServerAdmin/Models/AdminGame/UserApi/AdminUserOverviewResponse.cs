using System;
using System.Collections.Generic;

namespace GameServerAdmin.Models.AdminGame.UserApi
{
    /// <summary>
    /// 운영툴에서 보는 유저 상세 개요
    /// - 프로필 + 재화 + 스테이지 클리어 요약
    /// </summary>
    public class AdminUserOverviewResponse
    {
        // 기본 프로필
        public long UserId { get; set; }
        public long AccountId { get; set; }
        public string LoginId { get; set; } = null!;
        public string Nickname { get; set; } = null!;
        public long Level { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // 재화 요약
        public List<UserCurrencySummaryDto> Currencies { get; set; } = new();

        // 스테이지 클리어 요약
        public List<UserStageClearSummaryDto> StageClears { get; set; } = new();
    }
}