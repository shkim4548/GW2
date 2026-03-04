using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Common.Exceptions.Validation;
using GameServerAdmin.Common.Security;
using GameServerAdmin.Domain.Game.Stages;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.AdminGame.StageApi;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.AdminGame
{
    public interface IAdminStageService
    {
        Task<List<AdminStageListItemResponse>> GetStagesAsync();
        Task<AdminStageDetailResponse> GetStageAsync(int stageId);

        Task<AdminStageDetailResponse> CreateStageAsync(AdminCreateStageRequest request);
        Task<AdminStageDetailResponse> UpdateStageAsync(int stageId, AdminUpdateStageRequest request);

        Task EnableStageAsync(int stageId);
        Task DisableStageAsync(int stageId);
    }

    public sealed class AdminStageService : IAdminStageService
    {
        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;

        public AdminStageService(AppDbContext db, IUserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        public async Task<List<AdminStageListItemResponse>> GetStagesAsync()
        {
            EnsureAdminActor();

            var stages = await _db.Set<Stage>()
                .AsNoTracking()
                .OrderBy(s => s.StageId)
                .ToListAsync();

            return stages.Select(ToListItem).ToList();
        }

        public async Task<AdminStageDetailResponse> GetStageAsync(int stageId)
        {
            EnsureAdminActor();

            var stage = await _db.Set<Stage>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StageId == stageId);

            if (stage is null)
                throw new NotFoundException($"스테이지를 찾을 수 없습니다. (StageId: {stageId})");

            return ToDetail(stage);
        }

        public async Task<AdminStageDetailResponse> CreateStageAsync(AdminCreateStageRequest request)
        {
            EnsureAdminActor();

            if (request is null)
                throw new BadRequestException("요청이 잘못되었습니다.");

            var stage = new Stage(
                stageId: request.StageId,
                name: request.Name,
                requiredStamina: request.RequiredStamina,
                rewardGold: request.RewardGold,
                rewardGem: request.RewardGem);

            _db.Set<Stage>().Add(stage);
            await _db.SaveChangesAsync();

            return ToDetail(stage);
        }

        public async Task<AdminStageDetailResponse> UpdateStageAsync(int stageId, AdminUpdateStageRequest request)
        {
            EnsureAdminActor();

            if (request is null)
                throw new BadRequestException("요청이 잘못되었습니다.");

            var stage = await _db.Set<Stage>()
                .FirstOrDefaultAsync(s => s.StageId == stageId);

            if (stage is null)
                throw new NotFoundException($"스테이지를 찾을 수 없습니다. (StageId: {stageId})");

            stage.Update(
                request.Name,
                request.RequiredStamina,
                request.RewardGold,
                request.RewardGem);

            await _db.SaveChangesAsync();

            return ToDetail(stage);
        }

        public async Task EnableStageAsync(int stageId)
        {
            EnsureAdminActor();

            var stage = await _db.Set<Stage>()
                .FirstOrDefaultAsync(s => s.StageId == stageId);

            if (stage is null)
                throw new NotFoundException($"스테이지를 찾을 수 없습니다. (StageId: {stageId})");

            stage.Enable();
            await _db.SaveChangesAsync();
        }

        public async Task DisableStageAsync(int stageId)
        {
            EnsureAdminActor();

            var stage = await _db.Set<Stage>()
                .FirstOrDefaultAsync(s => s.StageId == stageId);

            if (stage is null)
                throw new NotFoundException($"스테이지를 찾을 수 없습니다. (StageId: {stageId})");

            stage.Disable();
            await _db.SaveChangesAsync();
        }

        private void EnsureAdminActor()
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedException("로그인이 필요합니다.");

            if (!string.Equals(_userContext.ActorType, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("관리자 전용 API입니다.");

            if (_userContext.ActorId <= 0)
                throw new UnauthorizedException("유효하지 않은 관리자입니다.");
        }

        private static AdminStageListItemResponse ToListItem(Stage stage)
        {
            return new AdminStageListItemResponse
            {
                StageId = stage.StageId,
                Name = stage.Name,
                RequiredStamina = stage.RequiredStamina,
                RewardGold = stage.RewardGold,
                RewardGem = stage.RewardGem,
                IsEnabled = stage.IsEnabled
            };
        }

        private static AdminStageDetailResponse ToDetail(Stage stage)
        {
            return new AdminStageDetailResponse
            {
                StageId = stage.StageId,
                Name = stage.Name,
                RequiredStamina = stage.RequiredStamina,
                RewardGold = stage.RewardGold,
                RewardGem = stage.RewardGem,
                IsEnabled = stage.IsEnabled,
                CreatedAt = stage.CreatedAt
            };
        }
    }
}