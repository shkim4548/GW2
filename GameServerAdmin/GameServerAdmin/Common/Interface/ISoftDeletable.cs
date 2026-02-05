namespace GameServerAdmin.Common.Interface
{
    public interface ISoftDeletable
    {
        DateTime? DeletedAt { get; }
        void SoftDelete();
        void Restore();
    }
}
