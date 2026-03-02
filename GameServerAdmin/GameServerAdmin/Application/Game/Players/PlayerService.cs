using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Common.Exceptions.Validation;
using GameServerAdmin.Domain.Users;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Game.PlayerApi;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Game.Players
{
    public interface IPlayerService
    {
        /// <summary>
        /// 현재 로그인한 플레이어의 프로필 조회
        /// </summary>
        Task<PlayerProfileResponse> GetMyProfileAsync();

        /// <summary>
        /// 현재 로그인한 플레이어의 닉네임 변경
        /// </summary>
        Task<PlayerProfileResponse> ChangeNicknameAsync(ChangeNicknameRequest request);
    }

    /// <summary>
    /// 게임 플레이어(유저) 관련 Application Service
    /// </summary>
    public sealed class PlayerService : IPlayerService
    {
        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;

        public PlayerService(AppDbContext db, IUserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        /// <summary>
        /// 현재 요청의 Actor가 정상적인 "User" 인지 검증하고
        /// User 엔티티를 조회한다.
        /// </summary>
        private async Task<User> GetCurrentUserAsync(bool tracking = false)
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedException("로그인이 필요합니다.");

            if (!string.Equals(_userContext.ActorType, "User", StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("일반 유저만 접근할 수 있습니다.");

            var userId = _userContext.ActorId;

            IQueryable<User> query = _db.Users;

            if (!tracking)
                query = query.AsNoTracking();

            var user = await query.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new PlayerNotFoundException(userId);

            return user;
        }

        private static PlayerProfileResponse ToProfile(User user)
        {
            return new PlayerProfileResponse
            {
                UserId = user.UserId,
                Nickname = user.Nickname,
                Level = user.Level,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Status = user.Status
            };
        }

        public async Task<PlayerProfileResponse> GetMyProfileAsync()
        {
            var user = await GetCurrentUserAsync(tracking: false);
            return ToProfile(user);
        }

        public async Task<PlayerProfileResponse> ChangeNicknameAsync(ChangeNicknameRequest request)
        {
            // ModelStateValidationFilter가 1차 검증은 해주지만,
            // 여기서는 도메인 관점에서 한 번 더 방어적으로 체크.
            if (string.IsNullOrWhiteSpace(request.NewNickname))
            {
                var fieldErrors = new FieldErrorCollection();
                fieldErrors.AddError(nameof(request.NewNickname), "닉네임은 비어 있을 수 없습니다.");
                throw new RequestValidationException(fieldErrors);
            }

            var trimmed = request.NewNickname.Trim();

            if (trimmed.Length is < 1 or > 20)
            {
                var fieldErrors = new FieldErrorCollection();
                fieldErrors.AddError(nameof(request.NewNickname), "닉네임 길이는 1~20자여야 합니다.");
                throw new RequestValidationException(fieldErrors);
            }

            // 추후 닉네임 중복 체크, 욕설 필터 등은 여기에서 확장 가능

            var user = await GetCurrentUserAsync(tracking: true);

            user.Nickname = trimmed;

            await _db.SaveChangesAsync();

            // 변경 후 최신 상태 반환
            return ToProfile(user);
        }
    }
}