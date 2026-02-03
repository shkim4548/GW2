
namespace GameServerAdmin.Common.Exceptions.Comment
{
    public sealed class ParentCommentNotFoundException : NotFoundException
    {
        public ParentCommentNotFoundException(long parentCommentId)
            : base(
                ErrorCode.PARENT_COMMENT_NOT_FOUND,
                $"상위 댓글을 찾을 수 없습니다. parentCommentId={parentCommentId}"
            )
        {
        }
    }
}