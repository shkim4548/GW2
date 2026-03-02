using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Domain.Game.Inventory;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Game.InventoryApi;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Game.Inventory
{
    public interface IInventoryService
    {
        /// <summary>
        /// 현재 로그인한 유저의 재화 상태 조회
        /// </summary>
        Task<InventoryStateResponse> GetMyCurrenciesAsync();
    }

    /// <summary>
    /// 게임 재화(골드/젬/스태미너) 관련 Application Service
    /// </summary>
    public sealed class InventoryService : IInventoryService
    {
        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;

        public InventoryService(AppDbContext db, IUserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        private long GetCurrentUserId()
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedException("로그인이 필요합니다.");

            if (!string.Equals(_userContext.ActorType, "User", StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("일반 유저만 인벤토리 API를 사용할 수 있습니다.");

            return _userContext.ActorId; // 여기서 ActorId는 User.UserId 여야 한다.
        }

        public async Task<InventoryStateResponse> GetMyCurrenciesAsync()
        {
            var userId = GetCurrentUserId();

            var currencies = await _db.Set<PlayerCurrency>()
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.CurrencyType)
                .ToListAsync();

            // 없으면 0으로 초기 생성 (선택)
            if (!currencies.Any())
            {
                currencies = new List<PlayerCurrency>
                {
                    new PlayerCurrency(userId, CurrencyType.Stamina, 0),
                    new PlayerCurrency(userId, CurrencyType.Gold, 0),
                    new PlayerCurrency(userId, CurrencyType.Gem, 0)
                };

                // 추후 바로 DB에 저장할 수도 있지만,
                // 여기서는 조회 API이므로, 저장은 다른 로직(스테이지, 보상 등)에서 맡기고
                // 응답만 0으로 돌려주는 것도 가능하다.
            }

            return new InventoryStateResponse
            {
                Currencies = currencies
                    .Select(c => new CurrencyDto
                    {
                        CurrencyType = c.CurrencyType,
                        Amount = c.Amount
                    })
                    .ToList()
            };
        }
    }
}