using GameServerAdmin.Application.AdminGame;
using GameServerAdmin.Models.AdminGame.UserApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.BackOffice
{
    /// <summary>
    /// 운영툴용 게임 유저 관리 API
    /// </summary>
    [ApiController]
    [Route("api/admin/game/users")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public sealed class AdminGameUserController : ControllerBase
    {
        private readonly IAdminGameUserService _service;

        public AdminGameUserController(IAdminGameUserService service)
        {
            _service = service;
        }

        /// <summary>
        /// 특정 유저의 게임 상태 개요 조회
        /// GET /api/admin/game/users/{userId}/overview
        /// </summary>
        [HttpGet("{userId:long}/overview")]
        public async Task<ActionResult<AdminUserOverviewResponse>> GetOverview(long userId)
        {
            var result = await _service.GetUserOverviewAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// 유저 검색 (닉네임 또는 UserId)
        /// GET /api/admin/game/users/search?nickname=xxx&userId=123
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<AdminUserSearchItemResponse>>> Search(
            [FromQuery] string? nickname,
            [FromQuery] long? userId)
        {
            var result = await _service.SearchUsersAsync(nickname, userId);
            return Ok(result);
        }

        /// <summary>
        /// 유저 인벤토리(재화) 조회
        /// GET /api/admin/game/users/{userId}/inventory
        /// </summary>
        [HttpGet("{userId:long}/inventory")]
        public async Task<ActionResult<AdminUserOverviewResponse>> GetInventory(long userId)
        {
            // overview에 재화가 포함되어 있으므로 재사용
            var result = await _service.GetUserOverviewAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// 재화 지급 / 회수
        /// POST /api/admin/game/users/{userId}/grant-currency
        /// </summary>
        [HttpPost("{userId:long}/grant-currency")]
        public async Task<ActionResult<AdminGrantCurrencyResponse>> GrantCurrency(
            [FromRoute] long userId,
            [FromBody] AdminGrantCurrencyRequest request)
        {
            var result = await _service.GrantCurrencyAsync(userId, request);
            return Ok(result);
        }

        /// <summary>
        /// 유저 밴
        /// POST /api/admin/game/users/{userId}/ban
        /// </summary>
        [HttpPost("{userId:long}/ban")]
        public async Task<IActionResult> Ban([FromRoute] long userId)
        {
            await _service.BanUserAsync(userId);
            return NoContent();
        }

        /// <summary>
        /// 유저 밴 해제
        /// POST /api/admin/game/users/{userId}/unban
        /// </summary>
        [HttpPost("{userId:long}/unban")]
        public async Task<IActionResult> Unban([FromRoute] long userId)
        {
            await _service.UnbanUserAsync(userId);
            return NoContent();
        }
    }
}