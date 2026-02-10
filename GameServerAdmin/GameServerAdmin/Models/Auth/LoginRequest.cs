using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Models.Auth
{
    public class LoginRequest
    {
        [Required]
        public string UserName { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}
