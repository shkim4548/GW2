namespace GameServerAdmin.Common.Exceptions.Post
{
    public sealed class InvalidPostStateException : InvalidStateException
    {
        public InvalidPostStateException(string message) : base(message) { }
    }
}
