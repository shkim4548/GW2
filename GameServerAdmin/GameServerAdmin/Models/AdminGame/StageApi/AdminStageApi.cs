namespace GameServerAdmin.Models.AdminGame.StageApi
{
    public sealed class AdminStageListItemResponse
    {
        public int StageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RequiredStamina { get; set; }
        public long RewardGold { get; set; }
        public long RewardGem { get; set; }
        public bool IsEnabled { get; set; }
    }

    public sealed class AdminStageDetailResponse
    {
        public int StageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RequiredStamina { get; set; }
        public long RewardGold { get; set; }
        public long RewardGem { get; set; }
        public bool IsEnabled { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public sealed class AdminCreateStageRequest
    {
        public int StageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RequiredStamina { get; set; }
        public long RewardGold { get; set; }
        public long RewardGem { get; set; }
    }

    public sealed class AdminUpdateStageRequest
    {
        public string Name { get; set; } = string.Empty;

        public int RequiredStamina { get; set; }

        public long RewardGold { get; set; }
        public long RewardGem { get; set; }
    }
}