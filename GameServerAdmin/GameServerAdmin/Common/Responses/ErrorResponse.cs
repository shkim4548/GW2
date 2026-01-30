namespace GameServerAdmin.Common.Responses
{
    public class ErrorResponse
    {
        public ErrorCode ErrorCode { get; set; } = default;
        public string Message { get; set; } = default;
        public string TraceId { get; set; } = default;
        public ErrorResponse(ErrorCode errorCode, string message, string traceId)
        {
            ErrorCode = errorCode;
            Message = message;
            TraceId = traceId;
        }
    }
}
