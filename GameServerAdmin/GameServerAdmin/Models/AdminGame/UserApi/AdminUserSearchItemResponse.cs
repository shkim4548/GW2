namespace GameServerAdmin.Models.AdminGame.UserApi
{
    /// <summary>
    /// 유저 검색 결과 단건 (목록용 간단 응답)
    /// </summary>
    public class AdminUserSearchItemResponse
    {
        public long UserId { get; set; }
        public string Nickname { get; set; } = null!;
        public long Level { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}