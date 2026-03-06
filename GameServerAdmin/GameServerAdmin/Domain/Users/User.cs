using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Domain.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("user")]
public class User
{
    [Key]
    [Column("user_id")]
    public long UserId { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("nickname")]
    public string Nickname { get; set; } = null!;

    [Column("level")]
    public long Level { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("status")]
    public UserStatus Status { get; set; }   // string → enum

    // Ban 관련 메서드
    public void Ban()
    {
        if (Status == UserStatus.Banned)
            throw new DomainException("이미 밴 상태입니다.");
        Status = UserStatus.Banned;
    }

    public void Unban()
    {
        if (Status != UserStatus.Banned)
            throw new DomainException("밴 상태가 아닙니다.");
        Status = UserStatus.Active;
    }
}