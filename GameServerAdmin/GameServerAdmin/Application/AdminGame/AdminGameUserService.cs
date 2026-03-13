using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Domain.Accounts;
using GameServerAdmin.Domain.Game.Inventory;
using GameServerAdmin.Domain.Game.Stages;
using GameServerAdmin.Domain.Users;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.AdminGame.UserApi;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.AdminGame
{
    public interface IAdminGameUserService
    {
        /// <summary>
        /// 특정 유저의 게임 상태 개요 조회 (프로필 + 재화 + 스테이지 클리어)
        /// </summary>
        Task<AdminUserOverviewResponse> GetUserOverviewAsync(long userId);
        Task<List<AdminUserSearchItemResponse>> SearchUsersAsync(string? nickname, long? userId);
        Task<AdminGrantCurrencyResponse> GrantCurrencyAsync(long userId, AdminGrantCurrencyRequest request);
        Task BanUserAsync(long userId);
        Task UnbanUserAsync(long userId);
        Task<(List<AdminUserSearchItemResponse> Users, int TotalCount)> GetAllUsersAsync(int page, int pageSize);

    }

    /// <summary>
    /// 운영툴용 게임 유저 관리 서비스
    /// </summary>
    public sealed class AdminGameUserService : IAdminGameUserService
    {
        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;
        private readonly ILogger<AdminGameUserService> _logger;

        public AdminGameUserService(AppDbContext db, IUserContext userContext, ILogger<AdminGameUserService> logger)
        {
            _db = db;
            _userContext = userContext;
            _logger = logger;
        }

        private void EnsureAdminActor()
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedException("로그인이 필요합니다.");

            if (_userContext.ActorType != ActorType.ADMIN)
                throw new ForbiddenException("관리자 계정만 접근할 수 있습니다.");
        }

        public async Task<AdminUserOverviewResponse> GetUserOverviewAsync(long userId)
        {
            EnsureAdminActor();

            if (userId <= 0)
                throw new BadRequestException("유효하지 않은 userId 입니다.");

            // User + Account 조인
            var userQuery =
                from u in _db.Set<User>().AsNoTracking()
                join a in _db.Set<Account>().AsNoTracking()
                    on u.AccountId equals a.AccountId
                where u.UserId == userId
                select new { User = u, Account = a };

            var ua = await userQuery.FirstOrDefaultAsync();
            if (ua == null)
                throw new NotFoundException("유저를 찾을 수 없습니다.");

            // 재화
            var currencies = await _db.Set<PlayerCurrency>()
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .ToListAsync();

            // 스테이지 클리어 요약
            var clearsQuery =
                from c in _db.Set<PlayerStageClear>().AsNoTracking()
                join s in _db.Set<Stage>().AsNoTracking()
                    on c.StageId equals s.StageId
                where c.UserId == userId
                group new { c, s } by new { c.StageId, s.Name } into g
                select new UserStageClearSummaryDto
                {
                    StageId = g.Key.StageId,
                    StageName = g.Key.Name,
                    ClearCount = g.Count(),
                    LastClearedAt = g.Max(x => (DateTime?)x.c.ClearedAt)
                };

            var stageClears = await clearsQuery
                .OrderBy(x => x.StageId)
                .ToListAsync();

            return new AdminUserOverviewResponse
            {
                UserId = ua.User.UserId,
                AccountId = ua.Account.AccountId,
                LoginId = ua.Account.LoginId,
                Nickname = ua.User.Nickname,
                Level = ua.User.Level,
                Status = ua.User.Status,
                CreatedAt = ua.User.CreatedAt,
                LastLoginAt = ua.User.LastLoginAt,
                Currencies = currencies
                    .Select(c => new UserCurrencySummaryDto
                    {
                        CurrencyType = (int)c.CurrencyType,
                        Amount = c.Amount
                    })
                    .ToList(),
                StageClears = stageClears
            };
        }
        public async Task<List<AdminUserSearchItemResponse>> SearchUsersAsync(string? nickname, long? userId)
        {
            EnsureAdminActor();

            // 둘 다 없으면 검색 조건 없음 — 빈 결과 반환 (전체 조회 방지)
            if (string.IsNullOrWhiteSpace(nickname) && userId is null)
                throw new BadRequestException("nickname 또는 userId 중 하나는 필수입니다.");

            var query = _db.Set<User>().AsNoTracking();

            if (userId.HasValue)
                query = query.Where(u => u.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(nickname))
                query = query.Where(u => u.Nickname.Contains(nickname));

            var users = await query
                .OrderBy(u => u.UserId)
                .Take(50)  // 최대 50건 제한
                .ToListAsync();

            return users.Select(u => new AdminUserSearchItemResponse
            {
                UserId = u.UserId,
                Nickname = u.Nickname,
                Level = u.Level,
                Status = u.Status.ToString(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            }).ToList();
        }

        public async Task<AdminGrantCurrencyResponse> GrantCurrencyAsync(long userId, AdminGrantCurrencyRequest request)
        {
            EnsureAdminActor();

            if (userId <= 0)
                throw new BadRequestException("유효하지 않은 userId입니다.");

            if (request.ChangeAmount == 0)
                throw new BadRequestException("ChangeAmount는 0일 수 없습니다.");

            // 유저 존재 확인
            var userExists = await _db.Set<User>().AsNoTracking()
                .AnyAsync(u => u.UserId == userId);
            if (!userExists)
                throw new NotFoundException($"유저를 찾을 수 없습니다. (UserId: {userId})");

            long beforeAmount = 0;
            long afterAmount = 0;

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var currency = await _db.Set<PlayerCurrency>()
                    .FirstOrDefaultAsync(c =>
                        c.UserId == userId &&
                        c.CurrencyType == request.CurrencyType);

                if (currency is null)
                {
                    currency = new PlayerCurrency(userId, request.CurrencyType, 0);
                    _db.Set<PlayerCurrency>().Add(currency);
                }

                beforeAmount = currency.Amount;

                if (request.ChangeAmount > 0)
                    currency.Add(request.ChangeAmount);
                else
                    currency.Subtract(Math.Abs(request.ChangeAmount)); // 도메인 규칙 적용

                afterAmount = currency.Amount;

                // Audit Log
                var log = new AdminCurrencyLog(
                    adminId: _userContext.ActorId,
                    userId: userId,
                    currencyType: request.CurrencyType,
                    changeAmount: request.ChangeAmount,
                    beforeAmount: beforeAmount,
                    afterAmount: afterAmount,
                    reason: request.Reason);

                _db.Set<AdminCurrencyLog>().Add(log);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                _logger.LogInformation("[Admin] Grant currency: AdminId={AdminId} UserId={UserId} Type={Type} Change={Change} Before={Before} After={After}", 
                    _userContext.ActorId, userId, request.CurrencyType, request.ChangeAmount, beforeAmount, afterAmount);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return new AdminGrantCurrencyResponse
            {
                UserId = userId,
                CurrencyType = request.CurrencyType,
                BeforeAmount = beforeAmount,
                AfterAmount = afterAmount,
                ChangeAmount = request.ChangeAmount
            };
        }

        public async Task BanUserAsync(long userId)
        {
            EnsureAdminActor();

            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new NotFoundException($"유저를 찾을 수 없습니다. (UserId: {userId})");

            user.Ban(); // 도메인 규칙: 이미 밴이면 DomainException
            await _db.SaveChangesAsync();
            _logger.LogWarning("[Admin] User banned: AdminId={AdminId} UserId={UserId}",
                _userContext.ActorId, userId);
        }

        public async Task UnbanUserAsync(long userId)
        {
            EnsureAdminActor();

            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new NotFoundException($"유저를 찾을 수 없습니다. (UserId: {userId})");

            user.Unban();
            await _db.SaveChangesAsync();

            _logger.LogInformation("[Admin] User unbanned: AdminId={AdminId} UserId={UserId}",
                _userContext.ActorId, userId);
        }

        public async Task<(List<AdminUserSearchItemResponse> Users, int TotalCount)> GetAllUsersAsync(int page, int pageSize)
        {
            EnsureAdminActor();

            var query = _db.Set<User>().AsNoTracking();
            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users.Select(u => new AdminUserSearchItemResponse
            {
                UserId = u.UserId,
                Nickname = u.Nickname,
                Level = u.Level,
                Status = u.Status.ToString(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            }).ToList(), totalCount);
        }
    }
}