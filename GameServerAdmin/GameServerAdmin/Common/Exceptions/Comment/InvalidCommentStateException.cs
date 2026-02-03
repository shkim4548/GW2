
namespace GameServerAdmin.Common.Exceptions.Comment
{
    public sealed class InvalidCommentStateException : InvalidStateException
    {
        public InvalidCommentStateException(string message)
            : base(message)
        {
        }

        // 특정 상황별 정적 팩토리 메서드
        public static InvalidCommentStateException AlreadyDeleted()
        {
            return new InvalidCommentStateException("이미 삭제된 댓글입니다.");
        }

        public static InvalidCommentStateException NotDeleted()
        {
            return new InvalidCommentStateException("삭제된 댓글만 복구할 수 있습니다.");
        }

        public static InvalidCommentStateException CannotUpdate()
        {
            return new InvalidCommentStateException("삭제된 댓글은 수정할 수 없습니다.");
        }
    }
}