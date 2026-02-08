using Microsoft.AspNetCore.Identity;

namespace GameServerAdmin.Domain.Identity
{
    public class AppUser : IdentityUser<long>
    {
        // 기존 시스템과 연결
        public long? AccountId { get; set; }
        public long? AdminId { get; set; }

        // 추가속성
        public string? NickName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // 사용자 타입 구분, User or Admin
        public string UserType { get; set; } = "User";
    }
}
