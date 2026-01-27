namespace GameServerAdmin.Common.Exceptions
{
    public class InvalidStateException : DomainException
    {
        public InvalidStateException(string message) : base(message) { }
    }
}
