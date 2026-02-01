namespace GameServerAdmin.Common.Exceptions.Post
{
    public sealed class DomainException : AppException
    {
        public DomainException(string message) : base(ErrorCode.VALIDATION_FAILED, message) { }

        public override int StatusCode => StatusCodes.Status409Conflict;
    }
}
