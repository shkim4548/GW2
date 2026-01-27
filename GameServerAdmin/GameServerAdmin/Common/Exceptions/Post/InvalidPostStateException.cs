namespace GameServerAdmin.Common.Exceptions.Post
{
    public class InvalidPostStateException : DomainException
    {
        public InvalidPostStateException(string message) : base(message) { }
    }
}
