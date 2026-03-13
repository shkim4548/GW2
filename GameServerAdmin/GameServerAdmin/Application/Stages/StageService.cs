using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Common.Exceptions.Validation;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Domain.Game.Inventory;
using GameServerAdmin.Domain.Game.Stages;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Game.StageApi;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GameServerAdmin.Application.Game.Stages
{
    public interface IStageService
    {
        Task<List<StageInfoDto>> GetEnabledStagesAsync();
        Task<EnterStageResponse> EnterStageAsync(EnterStageRequest request);
        Task<ClearStageResponse> ClearStageAsync(ClearStageRequest request);
    }

    /// <summary>
    /// 게임 클라이언트용 스테이지 유즈케이스 구현
    /// </summary>
    public sealed class StageService : IStageService
    {
        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;
        private readonly ILogger<StageService> _logger;

        public StageService(AppDbContext db, IUserContext userContext, ILogger<StageService> logger)
        {
            _db = db;
            _userContext = userContext;
            _logger = logger;
        }

        /// <summary>
        /// 사용 가능한 스테이지 목록 조회 (IsEnabled == true)
        /// </summary>
        public async Task<List<StageInfoDto>> GetEnabledStagesAsync()
        {
            var stages = await _db.Set<Stage>()
                .AsNoTracking()
                .Where(s => s.IsEnabled)
                .OrderBy(s => s.StageId)
                .ToListAsync();

            return stages.Select(ToStageInfo).ToList();
        }

        /// <summary>
        /// 스테이지 입장 처리 (스태미너 차감)
        /// - 동시성 제어: PlayerCurrency(Stamina)에 대한 낙관적 락 + 재시도
        /// </summary>
        public async Task<EnterStageResponse> EnterStageAsync(EnterStageRequest request)
        {
            if (request is null)
                //throw new RequestValidationException(FieldErrorCollection.Single("StageId", "요청이 잘못되었습니다."));
                throw new NotFoundException("요청이 잘못되었습니다");

            var userId = GetCurrentUserIdOrThrowForUser();

            // 1) 스테이지 존재 + 활성 여부 확인 (읽기 전용)
            var stage = await _db.Set<Stage>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StageId == request.StageId && s.IsEnabled);

            if (stage is null)
                throw new NotFoundException($"스테이지를 찾을 수 없습니다. (StageId: {request.StageId})");

            long remainingStamina = 0;

            // 2) 스태미너 차감은 RowVersion 충돌 가능성이 있으므로 재시도 패턴으로 감싼다.
            await ExecuteWithRetryAsync(async () =>
            {
                // 항상 "최신" Stamina를 로드해야 하므로 재시도마다 다시 쿼리
                var stamina = await _db.Set<PlayerCurrency>()
                    .FirstOrDefaultAsync(c =>
                        c.UserId == userId &&
                        c.CurrencyType == CurrencyType.Stamina);

                if (stamina is null)
                {
                    stamina = new PlayerCurrency(userId, CurrencyType.Stamina, 0);
                    _db.Set<PlayerCurrency>().Add(stamina);
                }

                // 도메인 규칙: 부족하면 예외
                if (stamina.Amount < stage.RequiredStamina)
                    throw new DomainException($"스태미너가 부족합니다. (현재:{stamina.Amount}, 필요:{stage.RequiredStamina})");

                stamina.Subtract(stage.RequiredStamina);
                remainingStamina = stamina.Amount;

                await _db.SaveChangesAsync();
            });

            _logger.LogInformation("Stage entered: UserId={UserId} StageId={StageId} RemainingStamina={Stamina}", userId, request.StageId, remainingStamina);

            return new EnterStageResponse
            {
                Stage = ToStageInfo(stage),
                RemainingStamina = remainingStamina
            };
        }

        /// <summary>
        /// 스테이지 클리어 처리 (보상 지급 + 클리어 로그)
        /// - 동시성 제어: PlayerCurrency(Gold/Gem)에 대한 낙관적 락 + 재시도
        /// </summary>
        public async Task<ClearStageResponse> ClearStageAsync(ClearStageRequest request)
        {
            if (request is null)
                //throw new RequestValidationException(FieldErrorCollection.Single("StageId", "요청이 잘못되었습니다."));
                throw new NotFoundException("요청이 잘못되었습니다");

            var userId = GetCurrentUserIdOrThrowForUser();

            // 1) 스테이지 존재 + 활성 여부 확인 (읽기 전용)
            var stage = await _db.Set<Stage>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StageId == request.StageId && s.IsEnabled);

            if (stage is null)
                throw new NotFoundException($"스테이지를 찾을 수 없습니다. (StageId: {request.StageId})");

            PlayerCurrency gold = null!;
            PlayerCurrency gem = null!;

            // 2) 보상 지급 + 클리어 로그를 하나의 트랜잭션에서 처리
            await ExecuteWithRetryAsync(async () =>
            {
                gold = await _db.Set<PlayerCurrency>()
                    .FirstOrDefaultAsync(c =>
                        c.UserId == userId &&
                        c.CurrencyType == CurrencyType.Gold);

                if (gold is null)
                {
                    gold = new PlayerCurrency(userId, CurrencyType.Gold, 0);
                    _db.Set<PlayerCurrency>().Add(gold);
                }

                gem = await _db.Set<PlayerCurrency>()
                    .FirstOrDefaultAsync(c =>
                        c.UserId == userId &&
                        c.CurrencyType == CurrencyType.Gem);

                if (gem is null)
                {
                    gem = new PlayerCurrency(userId, CurrencyType.Gem, 0);
                    _db.Set<PlayerCurrency>().Add(gem);
                }

                // 도메인 규칙: Add 내부에서 오버플로/음수 방어 (PlayerCurrency.Add)
                gold.Add(stage.RewardGold);
                gem.Add(stage.RewardGem);

                var clear = new PlayerStageClear(userId, stage.StageId);
                _db.Set<PlayerStageClear>().Add(clear);

                await _db.SaveChangesAsync();
            });

            // 3) 트랜잭션 밖에서 집계/조회 (읽기 전용)
            var totalClearCount = await _db.Set<PlayerStageClear>()
                .AsNoTracking()
                .CountAsync(c => c.UserId == userId && c.StageId == stage.StageId);

            // gold/gem는 위 트랜잭션에서 tracking 상태로 남아있지만,
            // 읽기 정확성을 위해 다시 NoTracking으로 읽어도 된다. (성능에 따라 선택)
            // 여기서는 gold/gem 로컬 변수의 Amount를 그대로 사용한다.

            _logger.LogInformation("Stage cleared: UserId={UserId} StageId={StageId} RewardGold={Gold} RewardGem={Gem}", userId, request.StageId, stage.RewardGold, stage.RewardGem);

            return new ClearStageResponse
            {
                Stage = ToStageInfo(stage),
                TotalClearCount = totalClearCount,
                TotalGold = gold.Amount,
                TotalGem = gem.Amount
            };
        }

        #region private helpers

        /// <summary>
        /// 현재 요청이 User Actor인지 확인하고, UserId를 반환한다.
        /// </summary>
        private long GetCurrentUserIdOrThrowForUser()
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedException("로그인이 필요합니다.");

            if (_userContext.ActorType != ActorType.USER)
                throw new ForbiddenException("플레이어 전용 API입니다.");

            if (_userContext.ActorId <= 0)
                throw new UnauthorizedException("유효하지 않은 사용자입니다.");

            return _userContext.ActorId;
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

        /// <summary>
        /// DbUpdateConcurrencyException 발생 시 일정 횟수까지 재시도하는 공통 유틸.
        /// - PlayerCurrency RowVersion 충돌 처리용.
        /// - 추후 공통 클래스로 분리 가능한 후보 (현재는 StageService 전용, 추정)
        /// </summary>
        private async Task ExecuteWithRetryAsync(Func<Task> action, int maxRetryCount = 3)
        {
            var retry = 0;

            while (true)
            {
                try
                {
                    await action();
                    return;
                }
                catch (DbUpdateConcurrencyException)
                {
                    _logger.LogWarning("Concurrency retry {Retry}/{Max}", retry, maxRetryCount);

                    retry++;
                    if (retry >= maxRetryCount)
                        throw;

                    // 필요하다면 여기서 짧게 delay를 줄 수도 있음 (예: await Task.Delay(10);)
                }
            }
        }

        #endregion
    }
}