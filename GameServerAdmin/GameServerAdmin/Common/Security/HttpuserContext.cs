using GameServerAdmin.Common.Exceptions;
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
            var httpContext = _accessor.HttpContext;
            var user = httpContext?.User;

            if (user == null || user.Identity?.IsAuthenticated != true)
                throw new UnauthorizedException("User is not authenticated.");

            // ClaimsPrincipalExtensions 사용
            return user.GetActorIdOrThrow();
        }
    }

    public ActorType ActorType
    {
        get
        {
            var httpContext = _accessor.HttpContext;
            var user = httpContext?.User;

            if (user == null || user.Identity?.IsAuthenticated != true)
                return ActorType.USER;

            return user.GetActorTypeOrDefault(ActorType.USER);
        }
    }
}