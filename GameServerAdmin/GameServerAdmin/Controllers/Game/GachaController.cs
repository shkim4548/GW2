using GameServerAdmin.Application.Game.Gacha;
using GameServerAdmin.Models.Game.Gacha;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Game;

/// <summary>
/// 가챠 API (플레이어 + 어드민)
/// </summary>
[ApiController]
[Tags("Game - Gacha")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class GachaController : ControllerBase
{
    private readonly IGachaService _gachaService;

    public GachaController(IGachaService gachaService)
    {
        _gachaService = gachaService;
    }

    // ─── 플레이어 API ─────────────────────────────────────────────────────────

    /// <summary>
    /// 1회 뽑기
    /// POST /api/gacha/{poolId}/pull/single
    /// </summary>
    [HttpPost("api/gacha/{poolId:long}/pull/single")]
    public async Task<ActionResult<GachaPullResponse>> PullSingle(long poolId)
    {
        var result = await _gachaService.PullSingleAsync(poolId);
        return Ok(result);
    }

    /// <summary>
    /// 10연챠
    /// POST /api/gacha/{poolId}/pull/ten
    /// </summary>
    [HttpPost("api/gacha/{poolId:long}/pull/ten")]
    public async Task<ActionResult<GachaPullResponse>> PullTen(long poolId)
    {
        var result = await _gachaService.PullTenAsync(poolId);
        return Ok(result);
    }

    /// <summary>
    /// 내 가챠 재화 조회
    /// GET /api/gacha/currency
    /// </summary>
    [HttpGet("api/gacha/currency")]
    public async Task<ActionResult<GachaCurrencyResponse>> GetCurrency()
    {
        var result = await _gachaService.GetMyCurrencyAsync();
        return Ok(result);
    }

    /// <summary>
    /// 내 뽑기 기록
    /// GET /api/gacha/history
    /// </summary>
    [HttpGet("api/gacha/history")]
    public async Task<ActionResult<List<GachaHistoryItem>>> GetHistory()
    {
        var result = await _gachaService.GetMyHistoryAsync();
        return Ok(result);
    }

    // ─── 어드민 API ───────────────────────────────────────────────────────────

    /// <summary>
    /// 가챠 풀 목록 (Admin)
    /// GET /api/admin/gacha/pools
    /// </summary>
    [HttpGet("api/admin/gacha/pools")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<GachaPoolResponse>>> GetPools()
    {
        var result = await _gachaService.GetPoolsAsync();
        return Ok(result);
    }

    /// <summary>
    /// 가챠 풀 생성 (Admin)
    /// POST /api/admin/gacha/pools
    /// </summary>
    [HttpPost("api/admin/gacha/pools")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GachaPoolResponse>> CreatePool(
        [FromBody] AdminCreatePoolRequest request)
    {
        var result = await _gachaService.CreatePoolAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// 재화 지급 (Admin)
    /// POST /api/admin/gacha/currency/grant
    /// </summary>
    [HttpPost("api/admin/gacha/currency/grant")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GrantCurrency(
        [FromBody] AdminGrantCurrencyRequest request)
    {
        await _gachaService.GrantCurrencyAsync(request);
        return Ok();
    }
}
