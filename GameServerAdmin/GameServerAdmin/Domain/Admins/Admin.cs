using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain.Admins
{
    /// <summary>
    /// 운영자(Admin) 도메인 엔티티
    /// - 인증은 Identity(AppUser)가 담당
    /// - 이 엔티티는 "운영자 메타데이터"와 권한/상태 관리를 담당
    /// </summary>
    [Table("admin")]
    public class Admin
    {
        /// <summary>Admin PK</summary>
        [Key]
        [Column("admin_id")]
        public long AdminId { get; set; }

        /// <summary>
        /// 운영자 로그인 ID
        /// - Identity(AppUser.UserName)와 1:1로 맞추는 것을 목표로 한다.
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("login_id")]
        public string LoginId { get; set; } = null!;

        /// <summary>
        /// (레거시/임시) 비밀번호 해시
        /// - 최종적으로는 Identity에서만 비밀번호를 관리하고,
        ///   이 필드는 더 이상 인증에 사용하지 않는 방향으로 간다.
        /// - 지금은 NOT NULL 제약을 깨지 않기 위해 빈 문자열 등으로 채워두는 용도.
        /// </summary>
        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = null!;

        /// <summary>
        /// 운영자 역할
        /// 예: "Super", "GM", "Support" 등
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Column("role")]
        public string Role { get; set; } = null!;

        /// <summary>계정 생성 시각(UTC)</summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>마지막 로그인 시각(UTC)</summary>
        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        /// <summary>활성 여부</summary>
        [Column("is_active")]
        public bool IsActive { get; set; }
    }
}