namespace GameServerAdmin.Common.Exceptions.Post
{
    public sealed class InvalidPostStateException : AppException
    {
        public InvalidPostStateException(string message) : base(ErrorCode.INVALID_STATE, message) { }

        public override int StatusCode => StatusCodes.Status406NotAcceptable;
    }
}
