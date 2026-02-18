using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Domain.Users;
using System.Security.Claims;

namespace GameServerAdmin.Common.Security;

public static class ClaimsPrincipalExtensions
{
    // JWT / Cookie 모두에서 "유저 식별자"를 얻기 위한 최소 공통 로직
    public static long GetActorIdOrThrow(this ClaimsPrincipal user)
    {
        // 1) 표준: NameIdentifier
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);

        // 2) JWT 표준: sub
        value ??= user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(value))
            throw new UnauthorizedException("Missing user id claim.");

        if (!long.TryParse(value, out var id))
            throw new UnauthorizedException("Invalid user id claim.");

        return id;
    }
}