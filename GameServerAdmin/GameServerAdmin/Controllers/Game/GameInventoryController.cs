using GameServerAdmin.Application.Game.Inventory;
using GameServerAdmin.Models.Game.InventoryApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Game
{
    /// <summary>
    /// 게임 클라이언트용 인벤토리(재화) API
    /// </summary>
    [ApiController]
    [Route("api/game/inventory")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public sealed class GameInventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public GameInventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// 현재 로그인한 유저의 재화 상태 조회
        /// GET /api/game/inventory/state
        /// </summary>
        [HttpGet("state")]
        public async Task<ActionResult<InventoryStateResponse>> GetState()
        {
            var result = await _inventoryService.GetMyCurrenciesAsync();
            return Ok(result);
        }
    }
}