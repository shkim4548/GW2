using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Common.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GameServerAdmin.Common.Filters;

public class HttpExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is AppException ex)
        {
            context.Result = new ObjectResult(new ErrorResponse
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message
            })
            {
                StatusCode = ex.StatusCode
            };
            return;
        }

        context.Result = new ObjectResult(new ErrorResponse
        {
            ErrorCode = "INTERNAL_SERVER_ERROR",
            Message = "Unexpected error occurred"
        })
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
    }
}
