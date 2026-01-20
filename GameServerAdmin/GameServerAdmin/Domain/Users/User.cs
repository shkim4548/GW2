using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain.Users
{
    [Table("user")]

    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("account_id")]
        public int AccountId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("nickname")]
        public string Nickname { get; set; } = null!;

        [Column("level")]
        public int Level { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        [Required]
        [Column("status")]
        public string Status { get; set; } = null!;
    }
}
