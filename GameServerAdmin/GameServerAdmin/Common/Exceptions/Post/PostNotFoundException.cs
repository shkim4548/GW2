
namespace GameServerAdmin.Common.Exceptions.Post
{
    public sealed class PostNotFoundException : NotFoundException
    {
        public PostNotFoundException(long postId)
            : base(
                ErrorCode.BOARD_POST_NOT_FOUND,
                $"게시글을 찾을 수 없습니다. postId={postId}"
            )
        {
        }
    }
}