
namespace GameServerAdmin.Common.Exceptions.Comment
{
    public sealed class PostNotFoundException : NotFoundException
    {
        public PostNotFoundException(long commentId)
            : base(
                ErrorCode.BOARD_POST_NOT_FOUND,
                $"댓글을 찾을 수 없습니다. commentId={commentId}"
            )
        {
        }
    }
}