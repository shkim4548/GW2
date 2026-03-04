using GameServerAdmin.Common.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain.Game.Stages
{
    /// <summary>
    /// PvE 스테이지 기본 정보
    /// </summary>
    [Table("stage")]
    public class Stage
    {
        protected Stage() { }

        public Stage(int stageId, string name, int requiredStamina, long rewardGold, long rewardGem)
        {
            if (stageId <= 0)
                throw new DomainException("StageId는 0보다 커야 합니다.");

            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("스테이지 이름은 비어 있을 수 없습니다.");

            if (requiredStamina < 0)
                throw new DomainException("필요 스태미너는 음수가 될 수 없습니다.");

            if (rewardGold < 0 || rewardGem < 0)
                throw new DomainException("보상 재화는 음수가 될 수 없습니다.");

            StageId = stageId;
            Name = name;
            RequiredStamina = requiredStamina;
            RewardGold = rewardGold;
            RewardGem = rewardGem;
            IsEnabled = true;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>스테이지 식별자 (비즈니스 키)</summary>
        [Key]
        [Column("stage_id")]
        public int StageId { get; private set; }

        [Required]
        [Column("name")]
        public string Name { get; private set; } = null!;

        /// <summary>입장에 필요한 스태미너</summary>
        [Column("required_stamina")]
        public int RequiredStamina { get; private set; }

        /// <summary>클리어 시 골드 보상</summary>
        [Column("reward_gold")]
        public long RewardGold { get; private set; }

        /// <summary>클리어 시 젬 보상</summary>
        [Column("reward_gem")]
        public long RewardGem { get; private set; }

        [Column("is_enabled")]
        public bool IsEnabled { get; private set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; private set; }

        public void Disable() => IsEnabled = false;
        public void Enable() => IsEnabled = true;

        public void Update(string name, int requiredStamina, long rewardGold, long rewardGem)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("스테이지 이름은 필수입니다.");

            if (requiredStamina < 0)
                throw new DomainException("RequiredStamina는 0 이상이어야 합니다.");

            if (rewardGold < 0 || rewardGem < 0)
                throw new DomainException("보상 값은 0 이상이어야 합니다.");

            Name = name;
            RequiredStamina = requiredStamina;
            RewardGold = rewardGold;
            RewardGem = rewardGem;
        }
    }
}