using GameServerAdmin.Common;
using Microsoft.AspNetCore.Http;

namespace GameServerAdmin.Common.Exceptions
{
    /// <summary>
    /// HTTP 400 - 잘못된 요청
    /// </summary>
    public sealed class BadRequestException : AppException
    {
        public BadRequestException(string message) : base(ErrorCode.BAD_REQUEST, message)
        {

        }

        public override int StatusCode => StatusCodes.Status400BadRequest;
    }
}