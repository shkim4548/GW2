namespace GameServerAdmin.Common.Exceptions
{
    public class NotFoundException : AppException
    {
        // 기본 생성자 (기존)
        public NotFoundException(string message)
            : base(ErrorCode.NOT_FOUND, message)
        {
        }

        // 커스텀 ErrorCode를 받는 생성자 (새로 추가)
        protected NotFoundException(ErrorCode errorCode, string message)
            : base(errorCode, message)
        {
        }

        public override int StatusCode => StatusCodes.Status404NotFound;
    }
}