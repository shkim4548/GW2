namespace GameServerAdmin.Common.Exceptions
{
    public class ValidationException : DomainException
    {
        protected ValidationException(string message) : base(message) { }
    }
}
