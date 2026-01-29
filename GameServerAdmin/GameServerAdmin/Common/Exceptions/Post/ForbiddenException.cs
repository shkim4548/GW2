namespace GameServerAdmin.Common.Exceptions.Post
{
    public sealed class ForbiddenException : AppException
    {
        public ForbiddenException(string message) : base(ErrorCode.FORBIDDEN, message) { }

        public override int StatusCode => StatusCodes.Status403Forbidden;

    }
}
