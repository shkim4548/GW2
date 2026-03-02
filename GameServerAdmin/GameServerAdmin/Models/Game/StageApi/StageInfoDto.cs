namespace GameServerAdmin.Models.Game.StageApi
{
    public class StageInfoDto
    {
        public int StageId { get; set; }
        public string Name { get; set; } = null!;
        public int RequiredStamina { get; set; }
        public long RewardGold { get; set; }
        public long RewardGem { get; set; }
        public bool IsEnabled { get; set; }
    }
}