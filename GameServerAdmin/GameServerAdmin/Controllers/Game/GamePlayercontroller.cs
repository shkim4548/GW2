using GameServerAdmin.Application.Game.Players;
using GameServerAdmin.Models.Game.PlayerApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Game
{
    /// <summary>
    /// 게임 클라이언트용 플레이어 API
    /// </summary>
    [ApiController]
    [Route("api/game/player")]
    [Tags("Game - Player")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // /api 요청은 Program.cs의 SmartAuth 때문에 기본적으로 JWT를 사용
    public sealed class GamePlayerController : ControllerBase
    {
        private readonly IPlayerService _playerService;

        public GamePlayerController(IPlayerService playerService)
        {
            _playerService = playerService;
        }

        /// <summary>
        /// 현재 로그인한 플레이어의 프로필 조회
        /// GET /api/game/player/profile
        /// </summary>
        [HttpGet("profile")]
        public async Task<ActionResult<PlayerProfileResponse>> GetProfile()
        {
            var profile = await _playerService.GetMyProfileAsync();
            return Ok(profile);
        }

        /// <summary>
        /// 현재 로그인한 플레이어의 닉네임 변경
        /// POST /api/game/player/nickname
        /// </summary>
        [HttpPost("nickname")]
        public async Task<ActionResult<PlayerProfileResponse>> ChangeNickname(
            [FromBody] ChangeNicknameRequest request)
        {
            var profile = await _playerService.ChangeNicknameAsync(request);
            return Ok(profile);
        }
    }
}