namespace GameServerAdmin.Common.Exceptions
{
    public sealed class ValidationException : AppException
    {
        protected ValidationException(string message) : base(ErrorCode.VALIDATION_FAILED, message) { }

        public override int StatusCode => StatusCodes.Status400BadRequest;
    }
}
