using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain
{
    [Table("account")]
    public class Account
    {
        [Key]
        [Column("accound_id")]
        public int AccountId { get; set; }

        [Required]
        [Column("login_id")]
        [MaxLength(50)]
        public string LoginId { get; set; } = null!;

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = null!;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        [Column("is_banned")]
        public bool IsBanned { get; set; }
    }
}
