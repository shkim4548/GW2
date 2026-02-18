
namespace GameServerAdmin.Application.Posts
{
    [Serializable]
    internal class PostNotFoundException : Exception
    {
        private long postId;

        public PostNotFoundException()
        {
        }

        public PostNotFoundException(long postId)
        {
            this.postId = postId;
        }

        public PostNotFoundException(string? message) : base(message)
        {
        }

        public PostNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}