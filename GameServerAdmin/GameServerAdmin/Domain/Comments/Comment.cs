using GameServerAdmin.Common.Exceptions.Comment;

namespace GameServerAdmin.Domain.Comments
{
    public class Comment
    {
        private const int MaxContentLength = 1000;

        public long Id { get; private set; }
        public long PostId { get; private set; }
        public long? ParentCommentId { get; private set; }
        public long AuthorId { get; private set; }
        public string Content { get; private set; } = string.Empty;
        public CommentStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        protected Comment() { }

        // 원댓글 생성자
        public Comment(long postId, long authorId, string content)
        {
            ValidateContent(content);

            PostId = postId;
            ParentCommentId = null;
            AuthorId = authorId;
            Content = content;
            Status = CommentStatus.Active;
            CreatedAt = DateTime.UtcNow;
        }

        // 대댓글 생성자
        public Comment(long postId, long parentCommentId, long authorId, string content)
        {
            ValidateContent(content);

            PostId = postId;
            ParentCommentId = parentCommentId;
            AuthorId = authorId;
            Content = content;
            Status = CommentStatus.Active;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateContent(string newContent)
        {
            if (Status == CommentStatus.Deleted)
            {
                throw InvalidCommentStateException.CannotUpdate();
            }

            ValidateContent(newContent);
            Content = newContent;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            if (Status == CommentStatus.Deleted)
            {
                throw InvalidCommentStateException.AlreadyDeleted();
            }

            Status = CommentStatus.Deleted;
            DeletedAt = DateTime.UtcNow;
        }

        public void Restore()
        {
            if (Status != CommentStatus.Deleted)
            {
                throw InvalidCommentStateException.NotDeleted();
            }

            Status = CommentStatus.Active;
            DeletedAt = null;
        }

        public bool IsReply()
        {
            return ParentCommentId.HasValue;
        }

        public bool IsAuthor(long authorId)
        {
            return AuthorId == authorId;
        }

        private void ValidateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("댓글 내용은 비어있을 수 없습니다.", nameof(content));
            }

            if (content.Length > MaxContentLength)
            {
                throw new ArgumentException(
                    $"댓글 내용은 {MaxContentLength}자를 초과할 수 없습니다.",
                    nameof(content));
            }
        }
    }
}