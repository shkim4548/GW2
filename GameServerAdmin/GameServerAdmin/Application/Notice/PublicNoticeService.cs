using GameServerAdmin.Common.Exceptions.Notice;
using GameServerAdmin.Domain.Notices;
using GameServerAdmin.Infrastructure.Persistence;
using GameServerAdmin.Models.Notices.PublicApi;
using Microsoft.EntityFrameworkCore;

namespace GameServerAdmin.Application.Notice
{
    public class PublicNoticeService
    {
        private readonly AppDbContext _db;

        public PublicNoticeService(AppDbContext db)
        {
            _db = db;
        }

        // 공지 사항 목록 조회 : 게시 중 + 노출 기간 내 + 활성 여부
        public async Task<NoticeListResponse> GetNoticeAsync()
        {
            var now  = DateTime.UtcNow;

            var notices = await _db.Notices
                .Where(n => n.Status == NoticeStatus.Published)
                .Where(n =>
                    (n.DisplayStartAt == null || n.DisplayStartAt <= now) &&
                    (n.DisplayEndAt == null || n.DisplayEndAt >= now))
                .OrderByDescending(n => n.IsPinned)  // 고정 공지 먼저
                .ThenByDescending(n => n.CreatedAt)
                .ToListAsync();
            
            var pinnedCount = notices.Count(n => n.IsPinned);
            return new NoticeListResponse
            {
                TotalCount = notices.Count,
                PinnedCount = pinnedCount,
                Notices = notices.Select(MapToNoticeResponse).ToList()
            };
        }

        // 공지사항 상세조회
        public async Task<NoticeResponse> GetNoticeDetailAsync(long noticeId)
        {
            var notice = await _db.Notices.FindAsync(noticeId);

            if (notice == null)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            // 게시되지 않은 공지사항은 조회 불가
            if (notice.Status != NoticeStatus.Published)
            {
                throw new NoticeNotFoundException(noticeId);
            }

            // 노출 기간 확인
            if (!notice.IsDisplayable())
            {
                throw new NoticeNotFoundException(noticeId);
            }

            // 조회수 증가
            notice.IncrementViewCount();
            await _db.SaveChangesAsync();

            return MapToNoticeResponse(notice);
        }

        // 상단 고정 공지사항만 조회
        public async Task<List<NoticeResponse>> GetPinnedNoticeAsync()
        {
            var now = DateTime.UtcNow;

            var notices = await _db.Notices
                .Where(n => n.Status == NoticeStatus.Published && n.IsPinned)
                .Where(n =>
                    (n.DisplayStartAt == null || n.DisplayStartAt <= now) &&
                    (n.DisplayEndAt == null || n.DisplayEndAt >= now))
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return notices.Select(MapToNoticeResponse).ToList();
        }

        // 카테고리별 공지사항 조회
        public async Task<NoticeListResponse> GetNoticeByCategoryAsync(NoticeCategory category)
        {
            var now = DateTime.UtcNow;

            var notices = await _db.Notices
                .Where(n => n.Status == NoticeStatus.Published && n.Category == category)
                .Where(n =>
                    (n.DisplayStartAt == null || n.DisplayStartAt <= now) &&
                    (n.DisplayEndAt == null || n.DisplayEndAt >= now))
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.CreatedAt)
                .ToListAsync();

            var pinnedCount = notices.Count(n => n.IsPinned);

            return new NoticeListResponse
            {
                TotalCount = notices.Count,
                PinnedCount = pinnedCount,
                Notices = notices.Select(MapToNoticeResponse).ToList()
            };
        }

        // mapping helper
        private NoticeResponse MapToNoticeResponse(Domain.Notices.Notice notice)
        {
            return new NoticeResponse
            {
                NoticeId = notice.NoticeId,
                Title = notice.Title,
                Content = notice.Content,
                Category = notice.Category,
                Priority = notice.Priority,
                IsPinned = notice.IsPinned,
                ViewCount = notice.ViewCount,
                IsCommentEnabled = notice.IsCommentEnabled,
                CreatedAt = notice.CreatedAt,
                UpdatedAt = notice.UpdatedAt
            };
        }
    }
}
