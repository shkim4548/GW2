namespace GameServerAdmin.Common.Exceptions
{
    public abstract class AppException : Exception
    {
        protected AppException(ErrorCode errorCode, string message) : base(message) 
        {
            ErrorCode = errorCode;
        }
        public abstract int StatusCode { get; }
        public ErrorCode ErrorCode { get; }
    }
}
