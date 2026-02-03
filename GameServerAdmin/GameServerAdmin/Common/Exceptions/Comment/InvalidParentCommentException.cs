namespace GameServerAdmin.Common.Exceptions.Comment
{
    public sealed class InvalidParentCommentException : DomainException
    {
        public InvalidParentCommentException(string message)
            : base(message)
        {
        }

        // 2단계 대댓글 방지용
        public static InvalidParentCommentException NestedReplyNotAllowed(long parentCommentId)
        {
            return new InvalidParentCommentException(
                $"대댓글에는 답글을 달 수 없습니다. parentCommentId={parentCommentId}"
            );
        }

        // 삭제된 댓글에 대댓글 방지용
        public static InvalidParentCommentException ParentDeleted(long parentCommentId)
        {
            return new InvalidParentCommentException(
                $"삭제된 댓글에는 답글을 달 수 없습니다. parentCommentId={parentCommentId}"
            );
        }
    }
}