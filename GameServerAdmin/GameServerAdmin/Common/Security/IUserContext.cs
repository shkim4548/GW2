namespace GameServerAdmin.Common.Security;

/// <summary>
/// 현재 요청의 Actor(유저/어드민)를 얻는 추상화.
/// - Controller가 Claims를 직접 파싱하지 않도록 분리.
/// </summary>
public interface IUserContext
{
    bool IsAuthenticated { get; }
    long ActorId { get; }          // 인증 필수 API에서는 항상 값이 있어야 함
    ActorType ActorType { get; }      // "User" / "Admin" 등 (추정: string 유지)
}
