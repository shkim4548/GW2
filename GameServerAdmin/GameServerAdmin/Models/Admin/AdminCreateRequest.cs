namespace GameServerAdmin.Models.Admin
{
    // DTO 역할 수행
    public class AdminCreateRequest
    {
        public string LoginId { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role {  get; set; } = null!;
    }
}
