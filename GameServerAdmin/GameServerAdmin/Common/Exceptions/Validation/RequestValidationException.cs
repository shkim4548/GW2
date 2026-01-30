namespace GameServerAdmin.Common.Exceptions.Validation
{
    public sealed class RequestValidationException : AppException
    {
        public FieldErrorCollection FieldErrors { get; }

        public RequestValidationException(FieldErrorCollection fieldErrors)
            : base(ErrorCode.VALIDATION_FAILED, "요청 값이 올바르지 않습니다.")
        {
            FieldErrors = fieldErrors;
        }

        public override int StatusCode => StatusCodes.Status400BadRequest;
    }
}
