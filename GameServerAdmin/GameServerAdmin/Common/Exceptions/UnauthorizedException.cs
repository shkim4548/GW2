
namespace GameServerAdmin.Common.Exceptions;

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized")
        : base(ErrorCode.UNAUTHORIZED, message)
    {
    }
    public override int StatusCode => StatusCodes.Status401Unauthorized;

}
