namespace GameServerAdmin.Common.Exceptions
{
    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(ErrorCode.NOT_FOUND, message) { }

        public override int StatusCode => StatusCodes.Status404NotFound;
    }
}
