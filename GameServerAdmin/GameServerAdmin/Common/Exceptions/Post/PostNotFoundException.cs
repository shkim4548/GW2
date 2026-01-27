namespace GameServerAdmin.Common.Exceptions.Post
{
    public class PostNotFoundException : AppException
    {
        public override string ErrorCode => "POST_NOT_FOUND";
        public override int StatusCode => StatusCodes.Status404NotFound;

        public PostNotFoundException(int postId)
            : base($"Post not found. postId={postId}") { }
    }
}
