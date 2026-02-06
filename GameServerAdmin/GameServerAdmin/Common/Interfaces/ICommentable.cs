namespace GameServerAdmin.Common.Interfaces
{
    public interface ICommentable
    {
        long Id { get; }
        bool IsCommentEnabled { get; }
        void EnableComments();
        void DisableComments();
    }
}
