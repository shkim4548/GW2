namespace GameServerAdmin.Models.Admins
{
    public class AdminResponse
    {
        public int AdminId { get; set; }
        public string LoginId { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
