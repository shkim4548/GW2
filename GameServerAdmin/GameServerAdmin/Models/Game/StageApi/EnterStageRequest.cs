using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Game.StageApi
{
    public class EnterStageRequest
    {
        [Range(1, int.MaxValue)]
        public int StageId { get; set; }
    }
}