using Microsoft.AspNetCore.Http;

namespace GameServerAdmin.Common.Security;

public sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpUserContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public bool IsAuthenticated
        => _accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public long ActorId
    {
        get
        {
            var user = _accessor.HttpContext?.User;
            if (user is null)
                throw new InvalidOperationException("HttpContext is not available.");

            // 레포에 이미 있는 ClaimsPrincipalExtensions를 사용
            return user.GetActorIdOrThrow();
        }
    }

    public string ActorType
    {
        get
        {
            // (추정) Public은 일반 유저 작성으로 취급.
            // 추후 Admin이 Public API로 쓰는 케이스가 있다면 Role 기반으로 분기 가능.
            return "User";
        }
    }
}
