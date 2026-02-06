using GameServerAdmin.Common.Exceptions.Notice;
using GameServerAdmin.Common.Interfaces;

namespace GameServerAdmin.Domain.Notices
{
    public class Notice : ICommentable, IViewCountable, ISoftDeletable
    {
        protected Notice() { }
        public Notice(string title, string content, long adminId, NoticeCategory category, NoticePriority priority)
        {
            ValidateTitle(title);
            ValidateContent(content);

            Title = title;
        }

        // 기본 속성
        public long NoticeId { get; private set; }
        public string Title { get; private set; } = null!;
        public string Content { get; private set; } = null!;
        public long AdminId { get; private set; }
        public NoticeCategory Category { get; private set; }
        public NoticePriority Priority { get; private set; }
        public NoticeStatus Status { get; private set; }

        // Notice 전용 기능
        public bool IsPinned { get; private set; }
        public DateTime? DisplayStartAt { get; private set; }
        public DateTime? DisplayEndAt { get; private set; }

        // 공통기능
        public int ViewCount { get; private set; }
        public bool IsCommentEnabled { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        // ICommentable 구현
        long ICommentable.Id => NoticeId;

        public void EnableComments()
        {
            IsCommentEnabled = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void DisableComments()
        {
            IsCommentEnabled = false;
            UpdatedAt = DateTime.UtcNow;
        }

        // IViewCountable 구현
        public void IncrementViewCount()
        {
            ViewCount++;
        }

        // Notice 전용
        public void Update(string title, string content)
        {
            if(Status == NoticeStatus.Deleted)
            {
                throw new InvalidNoticeStateException("삭제된 공지사항은 수정할 수 없습니다");
            }

            ValidateTitle(title);
            ValidateContent(content);

            Title = title;
            Content = content;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateDetails(string title, string content, NoticeCategory category, NoticePriority priority)
        {
            Update(title, content);
            Category = category;
            Priority = priority;
        }

        public void SetDisplayPeriod(DateTime? startAt, DateTime? endAt)
        {
            if(startAt.HasValue && endAt.HasValue && startAt > endAt)
            {
                throw new ArgumentException("시작일은 종료일보다 빨라야합니다");
            }
            DisplayStartAt = startAt;
            DisplayEndAt = endAt;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Publish()
        {
            if(Status == NoticeStatus.Published)
            {
                throw new InvalidNoticeStateException("이미 게시된 공지사항입니다.");
            }

            if(Status == NoticeStatus.Deleted)
            {
                throw new InvalidNoticeStateException("삭제된 공지사항은 게시할 수 없습니다.");
            }

            Status = NoticeStatus.Published;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Hide()
        {
            if (Status != NoticeStatus.Published)
            {
                throw new InvalidNoticeStateException("게시된 공지사항만 숨길 수 있습니다.");
            }
            Status = NoticeStatus.Hidden;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Pin()
        {
            if(Status != NoticeStatus.Published)
            {
                throw new InvalidNoticeStateException("게시된 공지사항만 고정할 수 있습니다.");
            }
            IsPinned = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Unpin()
        {
            IsPinned = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            if (Status == NoticeStatus.Deleted)
                throw new InvalidNoticeStateException("이미 삭제된 공지사항입니다.");

            Status = NoticeStatus.Deleted;
            DeletedAt = DateTime.UtcNow;
            IsPinned = false;
        }

        public void Restore()
        {
            if (Status != NoticeStatus.Deleted)
                throw new InvalidNoticeStateException("삭제되지 않은 공지사항은 복구할 수 없습니다.");

            Status = NoticeStatus.Draft;
            DeletedAt = null;
        }

        public bool IsDisplayable()
        {
            if (Status != NoticeStatus.Published)
                return false;

            var now = DateTime.UtcNow;

            if (DisplayStartAt.HasValue && now < DisplayStartAt.Value)
                return false;

            if (DisplayEndAt.HasValue && now > DisplayEndAt.Value)
                return false;

            return true;
        }

        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("제목은 필수입니다.", nameof(title));

            if (title.Length > 200)
                throw new ArgumentException("제목은 200자를 초과할 수 없습니다.", nameof(title));
        }

        private static void ValidateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("내용은 필수입니다.", nameof(content));
        }
    }
}
