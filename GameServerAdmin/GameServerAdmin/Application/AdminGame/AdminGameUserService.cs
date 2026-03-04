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
    }

    /// <summary>
    /// 운영툴용 게임 유저 관리 서비스
    /// </summary>
    public sealed class AdminGameUserService : IAdminGameUserService
    {
        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;

        public AdminGameUserService(AppDbContext db, IUserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        private void EnsureAdminActor()
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedException("로그인이 필요합니다.");

            if (!string.Equals(_userContext.ActorType, "Admin", StringComparison.OrdinalIgnoreCase))
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
    }
}