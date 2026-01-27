namespace GameServerAdmin.Common.Exceptions.Post
{
    public class ValidationException : DomainException
    {
        protected ValidationException(string message) : base(message) { }
    }
}
