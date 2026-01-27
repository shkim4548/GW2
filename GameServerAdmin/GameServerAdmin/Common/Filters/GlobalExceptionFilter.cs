using GameServerAdmin.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GameServerAdmin.Common.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var ex = context.Exception;

            var response = ex switch
            {
                NotFoundException => new ObjectResult(ex.Message)
                {
                    StatusCode = StatusCodes.Status404NotFound
                },

                ValidationException => new ObjectResult(ex.Message)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                },

                ForbiddenException => new ObjectResult(ex.Message)
                {
                    StatusCode = StatusCodes.Status403Forbidden
                },

                DomainException => new ObjectResult(ex.Message)
                {
                    StatusCode = StatusCodes.Status409Conflict
                },

                _ => new ObjectResult("Internal Server Error")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                }
            };

            context.Result = response;
            context.ExceptionHandled = true;
        }
    }
}
