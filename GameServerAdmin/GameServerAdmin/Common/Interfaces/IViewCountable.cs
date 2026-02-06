namespace GameServerAdmin.Common.Interfaces
{
    public interface IViewCountable
    {
        int ViewCount { get; }
        void IncrementViewCount();
    }
}
