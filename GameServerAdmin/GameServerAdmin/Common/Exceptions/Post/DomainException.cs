namespace GameServerAdmin.Common.Exceptions.Post
{
    public class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
    }
}
