
namespace GameServerAdmin.Common.Exceptions.Comment
{
    public sealed class CommentNotFoundException : NotFoundException
    {
        public CommentNotFoundException(long commentId)
            : base(
                ErrorCode.COMMENT_NOT_FOUND,
                $"댓글을 찾을 수 없습니다. commentId={commentId}"
            )
        {
        }
    }
}