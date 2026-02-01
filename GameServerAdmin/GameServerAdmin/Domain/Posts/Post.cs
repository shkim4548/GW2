using GameServerAdmin.Common.Exceptions.Post;

namespace GameServerAdmin.Domain.Posts
{
    public class Post
    {
        protected Post() { }

        public Post(string postType, string title, string content, string authorType, int authorId)
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
        }

        public int PostId { get; private set; }
        public string PostType { get; private set; } = null!;
        public string Title { get; private set; } = null!;
        public string Content { get; private set; } = null!;
        public string AuthorType { get; private set; } = null!;
        public int AuthorId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public bool IsDeleted { get; private set; }

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