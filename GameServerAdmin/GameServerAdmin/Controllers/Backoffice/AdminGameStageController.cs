using GameServerAdmin.Application.AdminGame;
using GameServerAdmin.Models.AdminGame.StageApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Backoffice
{
    [ApiController]
    [Route("api/admin/game/stages")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public sealed class AdminGameStageController : ControllerBase
    {
        private readonly IAdminStageService _service;

        public AdminGameStageController(IAdminStageService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<AdminStageListItemResponse>>> GetStages()
        {
            var result = await _service.GetStagesAsync();
            return Ok(result);
        }

        [HttpGet("{stageId:int}")]
        public async Task<ActionResult<AdminStageDetailResponse>> GetStage([FromRoute] int stageId)
        {
            var result = await _service.GetStageAsync(stageId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AdminStageDetailResponse>> CreateStage(
            [FromBody] AdminCreateStageRequest request)
        {
            var result = await _service.CreateStageAsync(request);
            return Ok(result);
        }

        [HttpPut("{stageId:int}")]
        public async Task<ActionResult<AdminStageDetailResponse>> UpdateStage(
            [FromRoute] int stageId,
            [FromBody] AdminUpdateStageRequest request)
        {
            var result = await _service.UpdateStageAsync(stageId, request);
            return Ok(result);
        }

        [HttpPost("{stageId:int}/enable")]
        public async Task<IActionResult> EnableStage([FromRoute] int stageId)
        {
            await _service.EnableStageAsync(stageId);
            return NoContent();
        }

        [HttpPost("{stageId:int}/disable")]
        public async Task<IActionResult> DisableStage([FromRoute] int stageId)
        {
            await _service.DisableStageAsync(stageId);
            return NoContent();
        }
    }
}