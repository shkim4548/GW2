using GameServerAdmin.Application.Game.Stages;
using GameServerAdmin.Models.Game.StageApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Game
{
    /// <summary>
    /// 게임 클라이언트용 스테이지 API
    /// </summary>
    [ApiController]
    [Route("api/game/stage")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public sealed class GameStageController : ControllerBase
    {
        private readonly IStageService _stageService;

        public GameStageController(IStageService stageService)
        {
            _stageService = stageService;
        }

        /// <summary>
        /// 사용 가능한 스테이지 목록
        /// GET /api/game/stage/list
        /// </summary>
        [HttpGet("list")]
        public async Task<ActionResult<List<StageInfoDto>>> GetList()
        {
            var stages = await _stageService.GetEnabledStagesAsync();
            return Ok(stages);
        }

        /// <summary>
        /// 스테이지 입장 (스태미너 차감)
        /// POST /api/game/stage/enter
        /// </summary>
        [HttpPost("enter")]
        public async Task<ActionResult<EnterStageResponse>> Enter(
            [FromBody] EnterStageRequest request)
        {
            var result = await _stageService.EnterStageAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// 스테이지 클리어 처리 (보상 지급)
        /// POST /api/game/stage/clear
        /// </summary>
        [HttpPost("clear")]
        public async Task<ActionResult<ClearStageResponse>> Clear(
            [FromBody] ClearStageRequest request)
        {
            var result = await _stageService.ClearStageAsync(request);
            return Ok(result);
        }
    }
}