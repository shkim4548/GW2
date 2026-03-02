using GameServerAdmin.Common.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain.Game.Stages
{
    /// <summary>
    /// 플레이어의 스테이지 클리어 기록
    /// </summary>
    [Table("player_stage_clear")]
    public class PlayerStageClear
    {
        protected PlayerStageClear() { }

        public PlayerStageClear(long userId, int stageId)
        {
            if (userId <= 0)
                throw new DomainException("유효하지 않은 UserId 입니다.");
            if (stageId <= 0)
                throw new DomainException("유효하지 않은 StageId 입니다.");

            UserId = userId;
            StageId = stageId;
            ClearedAt = DateTime.UtcNow;
        }

        [Key]
        [Column("player_stage_clear_id")]
        public long PlayerStageClearId { get; private set; }

        [Column("user_id")]
        public long UserId { get; private set; }

        [Column("stage_id")]
        public int StageId { get; private set; }

        [Column("cleared_at")]
        public DateTime ClearedAt { get; private set; }
    }
}