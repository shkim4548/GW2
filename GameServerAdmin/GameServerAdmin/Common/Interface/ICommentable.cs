namespace GameServerAdmin.Common.Interface
{
    public interface ICommentable
    {
        long Id { get; }
        bool IsCommentEnabled { get; }
        void EnableComments();
        void DisableComments();
    }
}
