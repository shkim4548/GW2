using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Domain.Game.Gacha;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Game.Gacha;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Game.Gacha;

public interface IGachaService
{
    // 플레이어 API
    Task<GachaPullResponse>     PullSingleAsync(long poolId);
    Task<GachaPullResponse>     PullTenAsync(long poolId);
    Task<GachaCurrencyResponse> GetMyCurrencyAsync();
    Task<List<GachaHistoryItem>> GetMyHistoryAsync();

    // 어드민 API
    Task<List<GachaPoolResponse>> GetPoolsAsync();
    Task<GachaPoolResponse>       CreatePoolAsync(AdminCreatePoolRequest request);
    Task                          GrantCurrencyAsync(AdminGrantCurrencyRequest request);
}

public sealed class GachaService : IGachaService
{
    private readonly AppDbContext _db;
    private readonly IUserContext _userContext;

    public GachaService(AppDbContext db, IUserContext userContext)
    {
        _db          = db;
        _userContext = userContext;
    }

    // ─── 플레이어 API ─────────────────────────────────────────────────────────

    public async Task<GachaPullResponse> PullSingleAsync(long poolId)
        => await ExecutePullAsync(poolId, PullType.Single, count: 1);

    public async Task<GachaPullResponse> PullTenAsync(long poolId)
        => await ExecutePullAsync(poolId, PullType.Ten, count: 10);

    public async Task<GachaCurrencyResponse> GetMyCurrencyAsync()
    {
        EnsureAuthenticated();
        var userId   = _userContext.ActorId;
        var currency = await _db.PlayerGachaCurrencies.FirstOrDefaultAsync(c => c.UserId == userId);
        return new GachaCurrencyResponse(userId, currency?.Amount ?? 0);
    }

    public async Task<List<GachaHistoryItem>> GetMyHistoryAsync()
    {
        EnsureAuthenticated();
        var userId = _userContext.ActorId;

        return await _db.GachaHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.PulledAt)
            .Join(_db.GachaItems,
                  h    => h.GachaItemId,
                  item => item.GachaItemId,
                  (h, item) => new GachaHistoryItem(
                      h.GachaHistoryId,
                      h.GachaItemId,
                      item.Name,
                      item.Grade,
                      h.PullType,
                      h.IsDuplicate,
                      h.CompensationAmount,
                      h.PulledAt))
            .ToListAsync();
    }

    // ─── 어드민 API ───────────────────────────────────────────────────────────

    public async Task<List<GachaPoolResponse>> GetPoolsAsync()
    {
        return await _db.GachaPools
            .AsNoTracking()
            .OrderByDescending(p => p.GachaPoolId)
            .Select(p => ToPoolResponse(p))
            .ToListAsync();
    }

    public async Task<GachaPoolResponse> CreatePoolAsync(AdminCreatePoolRequest request)
    {
        var pool = new GachaPool(
            request.Name,
            request.SinglePullCost,
            request.TenPullCost,
            request.PityThreshold,
            request.PityGrade,
            request.StartAt,
            request.EndAt);

        _db.GachaPools.Add(pool);
        await _db.SaveChangesAsync();
        return ToPoolResponse(pool);
    }

    public async Task GrantCurrencyAsync(AdminGrantCurrencyRequest request)
    {
        var currency = await _db.PlayerGachaCurrencies
            .FirstOrDefaultAsync(c => c.UserId == request.UserId);

        if (currency is null)
        {
            currency = new PlayerGachaCurrency(request.UserId);
            _db.PlayerGachaCurrencies.Add(currency);
        }

        currency.Add(request.Amount);
        await _db.SaveChangesAsync();
    }

    // ─── 핵심 뽑기 로직 ──────────────────────────────────────────────────────

    private async Task<GachaPullResponse> ExecutePullAsync(long poolId, PullType pullType, int count)
    {
        EnsureAuthenticated();
        var userId = _userContext.ActorId;
        var now    = DateTime.UtcNow;

        // 1. 풀 유효성 확인
        var pool = await _db.GachaPools
            .FirstOrDefaultAsync(p => p.GachaPoolId == poolId);
        if (pool is null || !pool.IsAvailable(now))
            throw new DomainException("사용할 수 없는 가챠 풀입니다.");

        // 2. 비용 계산 및 재화 차감
        var cost = pullType == PullType.Single ? pool.SinglePullCost : pool.TenPullCost;

        var currency = await _db.PlayerGachaCurrencies
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (currency is null)
        {
            currency = new PlayerGachaCurrency(userId);
            _db.PlayerGachaCurrencies.Add(currency);
        }
        currency.Subtract(cost);  // 부족 시 DomainException

        // 3. 풀 아이템 목록 로드
        var poolItems = await _db.GachaPoolItems
            .Include(pi => pi.Item)
            .Where(pi => pi.GachaPoolId == poolId && pi.Item!.IsActive)
            .ToListAsync();

        if (poolItems.Count == 0)
            throw new DomainException("풀에 활성화된 아이템이 없습니다.");

        // 4. 천장(pity) 조회 or 생성
        var pity = await _db.PlayerGachaPities
            .FirstOrDefaultAsync(p => p.UserId == userId && p.GachaPoolId == poolId);
        if (pity is null)
        {
            pity = new PlayerGachaPity(userId, poolId);
            _db.PlayerGachaPities.Add(pity);
        }

        // 5. 인벤토리 (중복 확인용) 미리 로드
        var inventoryItemIds = (await _db.PlayerGachaInventories
            .Where(inv => inv.UserId == userId)
            .Select(inv => inv.GachaItemId)
            .ToListAsync()).ToHashSet();

        // 6. count 번 뽑기 수행
        var results = new List<GachaPullResultItem>(count);
        var rng     = new Random();

        for (int i = 0; i < count; i++)
        {
            pity.Increment();

            // 천장 발동 여부 결정
            bool pityTriggered = pity.PullCount >= pool.PityThreshold;
            var  candidates    = pityTriggered
                ? poolItems.Where(pi => pi.Item!.Grade >= pool.PityGrade).ToList()
                : poolItems;

            if (candidates.Count == 0)
                candidates = poolItems;  // 천장 등급 해당 아이템 없으면 전체 fallback

            // 가중치 랜덤 선택
            var selectedItem = WeightedRandom(candidates, rng);

            if (pityTriggered)
                pity.Reset();

            // 중복 확인
            bool isDuplicate        = inventoryItemIds.Contains(selectedItem.GachaItemId);
            int  compensationAmount = 0;

            if (isDuplicate)
            {
                compensationAmount = selectedItem.Item!.DuplicateCompensation;
                currency.Add(compensationAmount);
            }
            else
            {
                var newInv = new PlayerGachaInventory(userId, selectedItem.GachaItemId);
                _db.PlayerGachaInventories.Add(newInv);
                inventoryItemIds.Add(selectedItem.GachaItemId);
            }

            // 기록
            _db.GachaHistories.Add(new GachaHistory(
                userId, poolId, selectedItem.GachaItemId,
                pullType, isDuplicate, compensationAmount));

            results.Add(new GachaPullResultItem(
                selectedItem.GachaItemId,
                selectedItem.Item!.Name,
                selectedItem.Item.Grade,
                isDuplicate,
                compensationAmount));
        }

        // 7. 단일 트랜잭션 저장
        await _db.SaveChangesAsync();

        return new GachaPullResponse(results, currency.Amount);
    }

    // ─── 헬퍼 ────────────────────────────────────────────────────────────────

    private static GachaPoolItem WeightedRandom(List<GachaPoolItem> items, Random rng)
    {
        int totalWeight = items.Sum(i => i.Weight);
        int roll        = rng.Next(1, totalWeight + 1);
        int cumulative  = 0;

        foreach (var item in items)
        {
            cumulative += item.Weight;
            if (roll <= cumulative)
                return item;
        }

        return items[^1];
    }

    private static GachaPoolResponse ToPoolResponse(GachaPool p) =>
        new(p.GachaPoolId, p.Name, p.SinglePullCost, p.TenPullCost,
            p.PityThreshold, p.PityGrade, p.IsActive, p.StartAt, p.EndAt);

    private void EnsureAuthenticated()
    {
        if (!_userContext.IsAuthenticated)
            throw new UnauthorizedAccessException("인증이 필요합니다.");
    }
}
