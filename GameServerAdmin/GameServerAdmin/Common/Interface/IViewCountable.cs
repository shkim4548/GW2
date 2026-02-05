namespace GameServerAdmin.Common.Interface
{
    public interface IViewCountable
    {
        int ViewCount { get; }
        void IncrementViewCount();
    }
}
