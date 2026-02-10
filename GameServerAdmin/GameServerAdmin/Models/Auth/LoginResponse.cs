namespace GameServerAdmin.Models.Auth
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }
        public string UserName { get; set; } = null!;
        public string[] Roles { get; set; } = [];
    }
}
