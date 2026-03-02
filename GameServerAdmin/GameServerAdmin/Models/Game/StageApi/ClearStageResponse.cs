namespace GameServerAdmin.Models.Game.StageApi
{
    public class ClearStageResponse
    {
        public StageInfoDto Stage { get; set; } = null!;
        /// <summary>해당 스테이지 누적 클리어 횟수</summary>
        public int TotalClearCount { get; set; }
        /// <summary>클리어 후 총 골드</summary>
        public long TotalGold { get; set; }
        /// <summary>클리어 후 총 젬</summary>
        public long TotalGem { get; set; }
    }
}