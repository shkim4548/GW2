using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Common.Exceptions.Post;
using GameServerAdmin.Common.Interfaces;

namespace GameServerAdmin.Domain.Posts
{
    public class Post : ICommentable, IViewCountable, ISoftDeletable
    {
        protected Post() { }

        public Post(string postType, string title, string content, string authorType, long authorId)
        {
            ValidateTitle(title);
            ValidateContent(content);

            PostType = postType;
            Title = title;
            Content = content;
            AuthorType = authorType;
            AuthorId = authorId;
            Status = PostStatus.Active;
            CreatedAt = DateTime.UtcNow;

            // 새로 추가된 속성 초기화
            ViewCount = 0;
            IsCommentEnabled = true;
        }

        // 기본 속성
        public long PostId { get; private set; }
        public string PostType { get; private set; } = null!;
        public string Title { get; private set; } = null!;
        public string Content { get; private set; } = null!;
        public string AuthorType { get; private set; } = null!;
        public long AuthorId { get; private set; }
        public PostStatus Status { get; private set; }
        public bool IsDeleted { get; private set; }

        // 공통 기능 속성 (새로 추가)
        public int ViewCount { get; private set; }
        public bool IsCommentEnabled { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        // ICommentable 구현
        long ICommentable.Id => PostId;

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

        // 기존 메서드
        public void Update(string title, string content)
        {
            if (Status == PostStatus.Deleted)
                throw new InvalidPostStateException("삭제된 게시글은 수정할 수 없습니다.");

            ValidateTitle(title);
            ValidateContent(content);

            Title = title;
            Content = content;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            if (Status == PostStatus.Deleted)
                throw new InvalidPostStateException("이미 삭제된 게시글입니다.");

            Status = PostStatus.Deleted;
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Restore()
        {
            if (Status != PostStatus.Deleted)
                throw new InvalidPostStateException("삭제되지 않은 게시글은 복구할 수 없습니다.");

            Status = PostStatus.Active;
            IsDeleted = false;                
            DeletedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }

        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("제목은 필수입니다.");

            if (title.Length > 200)
                throw new DomainException("제목은 200자를 초과할 수 없습니다.");
        }

        private static void ValidateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new DomainException("내용은 필수입니다.");
        }
    }
}