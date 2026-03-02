using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Common.Exceptions.Validation;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Domain.Game.Inventory;
using GameServerAdmin.Domain.Game.Stages;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Game.StageApi;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Game.Stages
{
    public interface IStageService
    {
        Task<List<StageInfoDto>> GetEnabledStagesAsync();
        Task<EnterStageResponse> EnterStageAsync(EnterStageRequest request);
        Task<ClearStageResponse> ClearStageAsync(ClearStageRequest request);
    }

    public sealed class StageService : IStageService
    {
        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;

        public StageService(AppDbContext db, IUserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        private long GetCurrentUserId()
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedException("로그인이 필요합니다.");

            if (!string.Equals(_userContext.ActorType, "User", StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("일반 유저만 스테이지 API를 사용할 수 있습니다.");

            return _userContext.ActorId; // User.UserId
        }

        private static StageInfoDto ToStageInfo(Stage stage)
        {
            return new StageInfoDto
            {
                StageId = stage.StageId,
                Name = stage.Name,
                RequiredStamina = stage.RequiredStamina,
                RewardGold = stage.RewardGold,
                RewardGem = stage.RewardGem,
                IsEnabled = stage.IsEnabled
            };
        }

        public async Task<List<StageInfoDto>> GetEnabledStagesAsync()
        {
            var stages = await _db.Set<Stage>()
                .AsNoTracking()
                .Where(s => s.IsEnabled)
                .OrderBy(s => s.StageId)
                .ToListAsync();

            return stages.Select(ToStageInfo).ToList();
        }

        public async Task<EnterStageResponse> EnterStageAsync(EnterStageRequest request)
        {
            var errors = new FieldErrorCollection();

            if (request.StageId <= 0)
                errors.AddError(nameof(request.StageId), "유효하지 않은 StageId 입니다.");

            if (errors.Count > 0)
                throw new RequestValidationException(errors);

            var userId = GetCurrentUserId();

            var stage = await _db.Set<Stage>()
                .FirstOrDefaultAsync(s => s.StageId == request.StageId);

            if (stage == null || !stage.IsEnabled)
                throw new NotFoundException("스테이지를 찾을 수 없습니다.");

            // 스태미너 가져오기
            var stamina = await _db.Set<PlayerCurrency>()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CurrencyType == CurrencyType.Stamina);

            if (stamina == null)
            {
                stamina = new PlayerCurrency(userId, CurrencyType.Stamina, 0);
                await _db.AddAsync(stamina);
            }

            if (stamina.Amount < stage.RequiredStamina)
                throw new DomainException("스태미너가 부족합니다.");

            stamina.Subtract(stage.RequiredStamina);

            await _db.SaveChangesAsync();

            return new EnterStageResponse
            {
                Stage = ToStageInfo(stage),
                RemainingStamina = stamina.Amount
            };
        }

        public async Task<ClearStageResponse> ClearStageAsync(ClearStageRequest request)
        {
            var errors = new FieldErrorCollection();

            if (request.StageId <= 0)
                errors.AddError(nameof(request.StageId), "유효하지 않은 StageId 입니다.");

            if (errors.Count > 0)
                throw new RequestValidationException(errors);

            var userId = GetCurrentUserId();

            var stage = await _db.Set<Stage>()
                .FirstOrDefaultAsync(s => s.StageId == request.StageId);

            if (stage == null || !stage.IsEnabled)
                throw new NotFoundException("스테이지를 찾을 수 없습니다.");

            // 골드
            var gold = await _db.Set<PlayerCurrency>()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CurrencyType == CurrencyType.Gold);

            if (gold == null)
            {
                gold = new PlayerCurrency(userId, CurrencyType.Gold, 0);
                await _db.AddAsync(gold);
            }

            gold.Add(stage.RewardGold);

            // 젬
            var gem = await _db.Set<PlayerCurrency>()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CurrencyType == CurrencyType.Gem);

            if (gem == null)
            {
                gem = new PlayerCurrency(userId, CurrencyType.Gem, 0);
                await _db.AddAsync(gem);
            }

            gem.Add(stage.RewardGem);

            // 클리어 기록 추가
            var clear = new PlayerStageClear(userId, stage.StageId);
            await _db.AddAsync(clear);

            await _db.SaveChangesAsync();

            var totalClearCount = await _db.Set<PlayerStageClear>()
                .AsNoTracking()
                .CountAsync(c => c.UserId == userId && c.StageId == stage.StageId);

            return new ClearStageResponse
            {
                Stage = ToStageInfo(stage),
                TotalClearCount = totalClearCount,
                TotalGold = gold.Amount,
                TotalGem = gem.Amount
            };
        }
    }
}