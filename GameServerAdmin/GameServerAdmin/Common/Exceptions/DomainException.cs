namespace GameServerAdmin.Common.Exceptions
{
    public sealed class DomainException : AppException
    {
        protected DomainException(string message) : base(ErrorCode.VALIDATION_FAILED, message) { }

        public override int StatusCode => StatusCodes.Status409Conflict;
    }
}
