namespace GameServerAdmin.Common.Responses
{
    public class ValidationErrorResponse : ErrorResponse
    {
        public IDictionary<string, List<string>> FieldErrors { get; }

        public ValidationErrorResponse(ErrorCode errorCode, string message, string traceId, IDictionary<string, List<string>> fieldError) : base(errorCode, message, traceId)
        {
            FieldErrors = fieldError;
        }
    }
}
