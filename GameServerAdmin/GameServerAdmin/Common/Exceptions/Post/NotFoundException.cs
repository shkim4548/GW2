namespace GameServerAdmin.Common.Exceptions.Post
{
    public class NotFoundException : AppException
    {
        protected NotFoundException(ErrorCode errorCode, string message)
                    : base(errorCode, message)
        {
        }
        public override int StatusCode => StatusCodes.Status404NotFound;
    }
}
