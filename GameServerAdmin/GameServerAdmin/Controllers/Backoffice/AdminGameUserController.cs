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
    }
}