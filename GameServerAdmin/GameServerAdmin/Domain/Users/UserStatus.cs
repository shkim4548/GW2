namespace GameServerAdmin.Domain.Users
{
    public enum UserStatus
    {
        Active = 0,
        Banned = 1,
        Dormant = 2   // 선택. 지금 당장 쓰지 않아도 선언만 해두면 됨
    }
}