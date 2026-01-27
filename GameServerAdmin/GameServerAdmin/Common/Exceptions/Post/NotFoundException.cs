namespace GameServerAdmin.Common.Exceptions.Post
{
    public class NotFoundException : DomainException
    {
        public NotFoundException(string message) : base(message) { }
    }
}
