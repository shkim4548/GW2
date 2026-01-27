namespace GameServerAdmin.Common.Exceptions
{
    public abstract class AppException : Exception
    {
        public abstract string ErrorCode { get; }
        public abstract int StatusCode { get; }

        protected AppException(string message) : base(message) { }
    }
}
