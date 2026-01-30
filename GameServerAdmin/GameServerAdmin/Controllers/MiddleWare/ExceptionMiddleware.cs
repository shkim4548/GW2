using GameServerAdmin.Common;
using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Common.Exceptions.Validation;
using GameServerAdmin.Common.Responses;
using System.ComponentModel.DataAnnotations;

namespace GameServerAdmin.Controllers.MiddleWare
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch(RequestValidationException ex)
            {
                await HandleValidationException(context, ex);
            }
            catch (AppException ex)
            {
                await HandleAppException(context, ex);
            }
            catch (Exception ex)
            {
                await HandleUnknownException(context, ex);
            }
        }

        private async Task HandleAppException(HttpContext context, AppException ex)
        {
            _logger.LogWarning(ex, "Handled AppException");

            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse(ex.ErrorCode, ex.Message, context.TraceIdentifier);

            await context.Response.WriteAsJsonAsync(response);
        }

        private async Task HandleUnknownException(HttpContext context, Exception ex)
        {
            _logger.LogError(ex, "Unhandled Exception");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse(ErrorCode.INTERNAL_ERROR, "서버 내부 오류가 발생했습니다", context.TraceIdentifier);
            await context.Response.WriteAsJsonAsync(response);
        }

        private async Task HandleValidationException(HttpContext context, RequestValidationException ex)
        {
            _logger.LogInformation(ex, "Handle ValidationException");

            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType= "application/json";

            var responses = new ValidationErrorResponse(ex.ErrorCode, ex.Message, context.TraceIdentifier, ex.FieldErrors);

            await context.Response.WriteAsJsonAsync(responses);
        }
    }
}
