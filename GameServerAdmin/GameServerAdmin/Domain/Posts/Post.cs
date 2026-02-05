using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Common.Exceptions.Post;
using GameServerAdmin.Common.Interface;

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

            IsDeleted = false;
            CreatedAt = DateTime.UtcNow;

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
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public PostStatus Status { get; private set; }

        // 공통 기능
        public int ViewCount { get; private set; }
        public bool IsCommentEnabled { get; private set; }

        // ICommentable 구현
        long ICommentable.Id => PostId;

        public void EnableComments()
        {
            IsCommentEnabled = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void DisableComments()
        {
            IsCommentEnabled = true;
            UpdatedAt = DateTime.UtcNow;
        }

        // IViewCountable 구현
        public void IncrementViewCount()
        {
            ViewCount++;
        }

        public void Update(string title, string content)
        {
            if (IsDeleted)
                throw new InvalidPostStateException("Deleted post cannot be updated");

            ValidateTitle(title);
            ValidateContent(content);

            Title = title;
            Content = content;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            if (IsDeleted)
                throw new InvalidPostStateException("Already deleted");

            IsDeleted = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Restore()
        {
            if (!IsDeleted)
                throw new InvalidPostStateException("Post is not deleted");

            IsDeleted = false;
            UpdatedAt = DateTime.UtcNow;
        }

        private static void ValidateTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Title is required");
        }

        private static void ValidateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new DomainException("Content is required");
        }
    }
}