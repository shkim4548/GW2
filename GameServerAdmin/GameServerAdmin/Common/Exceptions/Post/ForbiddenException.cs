namespace GameServerAdmin.Common.Exceptions.Post
{
    public class ForbiddenException : DomainException
    {
        public ForbiddenException(string message) : base(message) { }
    }
}
