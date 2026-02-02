namespace GameServerAdmin.Common.Exceptions.Validation
{
    public sealed class InvalidStateException : AppException
    {
        public InvalidStateException(string message) : base(ErrorCode.INVALID_STATE, message) { }

        public override int StatusCode => StatusCodes.Status409Conflict;
    }
}
