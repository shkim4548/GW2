using GameServerAdmin.Common.Exceptions.Notice;
using GameServerAdmin.Common.Models;
using GameServerAdmin.Domain.Notices;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Notices.AdminApi;
using GameServerAdmin.Models.Posts.AdminApi;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Notice
{
    public interface IAdminNoticeService
    {
        Task<AdminNoticeResponse> CreateNoticeAsync(long adminId, CreateNoticeRequest request);
        Task<AdminNoticeResponse> UpdateNoticeAsync(long noticeId, UpdateNoticeRequest request);
        Task PublishNoticeAsync(long noticeId);
        Task HideNoticeAsync(long noticeId);
        Task PinNoticeAsync(long noticeId);
        Task UnpinNoticeAsync(long noticeId);
        Task EnableCommentsAsync(long noticeId);
        Task DisableCommentsAsync(long noticeId);
        Task DeleteNoticeAsync(long noticeId);
        Task RestoreNoticeAsync(long noticeId);
        Task HardDeleteNoticeAsync(long noticeId);
        Task<AdminNoticeResponse> GetNoticeDetailAsync(long noticeId);
        Task<PagedResponse<AdminNoticeListItemDto>> GetAllNoticesAsync(AdminNoticeListQuery query);
    }

    public class AdminNoticeService : IAdminNoticeService
    {
        private readonly AppDbContext _db;

        public AdminNoticeService(AppDbContext db)
        {
            _db = db;
        }

        // 공지사항 작성
        public async Task<AdminNoticeResponse> CreateNoticeAsync(long adminId, CreateNoticeRequest request)
        {
            var notice = new Domain.Notices.Notice(
                request.Title,
                request.Content,
                adminId,
                request.Category,
                request.Priority
            );

            if (request.DisplayStartAt.HasValue || request.DisplayEndAt.HasValue)
            {
                notice.SetDisplayPeriod(request.DisplayStartAt, request.DisplayEndAt);
            }

            _db.Notices.Add(notice);
            await _db.SaveChangesAsync();

            return MapToAdminNoticeResponse(notice);
        }

        /// <summary>
        /// 공지사항 수정
        /// </summary>
        public async Task<AdminNoticeResponse> UpdateNoticeAsync(long noticeId, UpdateNoticeRequest request)
        {
            var notice = await _db.Notices.FindAsync(noticeId);

            if (notice == null)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            notice.UpdateDetails(
                request.Title,
                request.Content,
                request.Category,
                request.Priority
            );

            notice.SetDisplayPeriod(request.DisplayStartAt, request.DisplayEndAt);

            await _db.SaveChangesAsync();

            return MapToAdminNoticeResponse(notice);
        }

        // 공지사항 게시 (Draft → Published)
        public async Task PublishNoticeAsync(long noticeId)
        {
            var notice = await _db.Notices.FindAsync(noticeId);

            if (notice == null)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            notice.Publish();
            await _db.SaveChangesAsync();
        }

        
        // 공지사항 숨김 (Published → Hidden)
        public async Task HideNoticeAsync(long noticeId)
        {
            var notice = await _db.Notices.FindAsync(noticeId);

            if (notice == null)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            notice.Hide();
            await _db.SaveChangesAsync();
        }

        // 공지사항 상단 고정
        public async Task PinNoticeAsync(long noticeId)
        {
            var notice = await _db.Notices.FindAsync(noticeId);

            if (notice == null)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            notice.Pin();
            await _db.SaveChangesAsync();
        }

        // 공지사항 고정 해제
        public async Task UnpinNoticeAsync(long noticeId)
        {
            var notice = await _db.Notices.FindAsync(noticeId);

            if (notice == null)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            notice.Unpin();
            await _db.SaveChangesAsync();
        }

        // 댓글 활성화
        public async Task EnableCommentsAsync(long noticeId)
        {
            var notice = await _db.Notices.FindAsync(noticeId);

            if (notice == null)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            notice.EnableComments();
            await _db.SaveChangesAsync();
        }

        // 댓글 비활성화
        public async Task DisableCommentsAsync(long noticeId)
        {
            var notice = await _db.Notices.FindAsync(noticeId);

            if (notice == null)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            notice.DisableComments();
            await _db.SaveChangesAsync();
        }

        // 공지사항 삭제 (Soft Delete)
        public async Task DeleteNoticeAsync(long noticeId)
        {
            var notice = await _db.Notices.FindAsync(noticeId);

            if (notice == null)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            notice.SoftDelete();
            await _db.SaveChangesAsync();
        }

        // 공지사항 복구
        public async Task RestoreNoticeAsync(long noticeId)
        {
            var notice = await _db.Notices.FindAsync(noticeId);

            if (notice == null)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            notice.Restore();
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// 공지사항 영구 삭제 (Hard Delete)
        /// </summary>
        public async Task HardDeleteNoticeAsync(long noticeId)
        {
            var notice = await _db.Notices.FindAsync(noticeId);

            if (notice == null)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            _db.Notices.Remove(notice);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// 공지사항 상세 조회 (Admin용 - 상태 무관)
        /// </summary>
        public async Task<AdminNoticeResponse> GetNoticeDetailAsync(long noticeId)
        {
            var notice = await _db.Notices.FindAsync(noticeId);

            if (notice == null)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            return MapToAdminNoticeResponse(notice);
        }

        /// <summary>
        /// 전체 공지사항 목록 조회 (필터링/페이징)
        /// </summary>
        public async Task<PagedResponse<AdminNoticeListItemDto>> GetAllNoticesAsync(AdminNoticeListQuery query)
        {
            var queryable = _db.Notices.AsQueryable();

            // 필터링
            if (query.Category.HasValue)
            {
                queryable = queryable.Where(n => n.Category == query.Category.Value);
            }

            if (query.Priority.HasValue)
            {
                queryable = queryable.Where(n => n.Priority == query.Priority.Value);
            }

            if (query.Status.HasValue)
            {
                queryable = queryable.Where(n => n.Status == query.Status.Value);
            }

            if (!query.IncludeDeleted)
            {
                queryable = queryable.Where(n => n.Status != NoticeStatus.Deleted);
            }

            // 총 개수
            var totalCount = await queryable.CountAsync();

            // 정렬
            queryable = query.SortBy.ToLower() switch
            {
                "updatedat" => query.SortOrder.ToLower() == "asc"
                    ? queryable.OrderBy(n => n.UpdatedAt)
                    : queryable.OrderByDescending(n => n.UpdatedAt),
                "viewcount" => query.SortOrder.ToLower() == "asc"
                    ? queryable.OrderBy(n => n.ViewCount)
                    : queryable.OrderByDescending(n => n.ViewCount),
                "priority" => query.SortOrder.ToLower() == "asc"
                    ? queryable.OrderBy(n => n.Priority)
                    : queryable.OrderByDescending(n => n.Priority),
                _ => query.SortOrder.ToLower() == "asc"
                    ? queryable.OrderBy(n => n.CreatedAt)
                    : queryable.OrderByDescending(n => n.CreatedAt)
            };

            // 페이징
            var items = await queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var itemDtos = items.Select(n => new AdminNoticeListItemDto
            {
                NoticeId = n.NoticeId,
                Title = n.Title,
                AdminId = n.AdminId,
                Category = n.Category,
                Priority = n.Priority,
                Status = n.Status,
                IsPinned = n.IsPinned,
                ViewCount = n.ViewCount,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt,
                DeletedAt = n.DeletedAt
            }).ToList();

            return new PagedResponse<AdminNoticeListItemDto>
            {
                Items = itemDtos,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        #region Mapping

        private AdminNoticeResponse MapToAdminNoticeResponse(Domain.Notices.Notice notice)
        {
            return new AdminNoticeResponse
            {
                NoticeId = notice.NoticeId,
                Title = notice.Title,
                Content = notice.Content,
                AdminId = notice.AdminId,
                Category = notice.Category,
                Priority = notice.Priority,
                Status = notice.Status,
                IsPinned = notice.IsPinned,
                DisplayStartAt = notice.DisplayStartAt,
                DisplayEndAt = notice.DisplayEndAt,
                ViewCount = notice.ViewCount,
                IsCommentEnabled = notice.IsCommentEnabled,
                CreatedAt = notice.CreatedAt,
                UpdatedAt = notice.UpdatedAt,
                DeletedAt = notice.DeletedAt
            };
        }

        #endregion
    }
}
