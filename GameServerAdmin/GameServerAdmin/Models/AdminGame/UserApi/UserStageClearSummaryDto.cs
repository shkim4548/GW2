namespace GameServerAdmin.Models.AdminGame.UserApi
{
    public class UserStageClearSummaryDto
    {
        /// <summary>스테이지 ID</summary>
        public int StageId { get; set; }

        /// <summary>스테이지 이름</summary>
        public string StageName { get; set; } = null!;

        /// <summary>누적 클리어 횟수</summary>
        public int ClearCount { get; set; }

        /// <summary>최근 클리어 시각(UTC)</summary>
        public DateTime? LastClearedAt { get; set; }
    }
}