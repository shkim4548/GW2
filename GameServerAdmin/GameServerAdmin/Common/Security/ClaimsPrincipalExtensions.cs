using GameServerAdmin.Common.Exceptions;
using System.Security.Claims;

namespace GameServerAdmin.Common.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// 도메인 기준 ActorId(UserId 또는 AdminId)를 가져온다.
    /// 우선순위:
    ///  1) actor_id 클레임
    ///  2) (호환용) NameIdentifier / sub
    /// </summary>
    public static long GetActorIdOrThrow(this ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            throw new UnauthorizedException("User is not authenticated.");

        // 1) 새 방식: actor_id
        var value = user.FindFirstValue("actor_id");

        // 2) 과거 호환용: NameIdentifier / sub (AppUser.Id)
        if (string.IsNullOrWhiteSpace(value))
        {
            value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub");
        }

        if (string.IsNullOrWhiteSpace(value))
            throw new UnauthorizedException("Missing actor id claim.");

        if (!long.TryParse(value, out var id))
            throw new UnauthorizedException("Invalid actor id claim.");

        return id;
    }

    /// <summary>
    /// 도메인 기준 ActorType("User" / "Admin")을 가져온다.
    /// 우선순위:
    ///  1) actor_type
    ///  2) user_type (과거 호환용)
    ///  3) 기본값
    /// </summary>
    public static string GetActorTypeOrDefault(this ClaimsPrincipal user, string defaultValue = "User")
    {
        if (user.Identity?.IsAuthenticated != true)
            return defaultValue;

        var value = user.FindFirstValue("actor_type")
                    ?? user.FindFirstValue("user_type");

        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return value;
    }
}