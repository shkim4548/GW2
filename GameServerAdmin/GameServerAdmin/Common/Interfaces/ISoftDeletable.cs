namespace GameServerAdmin.Common.Interfaces
{
    public interface ISoftDeletable
    {
        DateTime? DeletedAt { get; }
        void SoftDelete();
        void Restore();
    }
}
