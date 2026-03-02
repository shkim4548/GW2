namespace GameServerAdmin.Models.Game.StageApi
{
    public class EnterStageResponse
    {
        public StageInfoDto Stage { get; set; } = null!;
        /// <summary>입장 후 남은 스태미너</summary>
        public long RemainingStamina { get; set; }
    }
}