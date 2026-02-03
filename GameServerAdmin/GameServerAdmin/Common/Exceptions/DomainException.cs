namespace GameServerAdmin.Common.Exceptions
{
    public class DomainException : AppException
    {
        public DomainException(string message) : base(ErrorCode.VALIDATION_FAILED, message) { }

        public override int StatusCode => StatusCodes.Status409Conflict;
    }
}
