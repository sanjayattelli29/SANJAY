using Application.Common.Exceptions;
using Application.Common.Models;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
// please fix this 
namespace API.Middlewares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            var (statusCode, message, errors) = exception switch
            {
                NotFoundException => (HttpStatusCode.NotFound, exception.Message, null),
                BadRequestException => (HttpStatusCode.BadRequest, exception.Message, null),
                UnauthorizedException => (HttpStatusCode.Unauthorized, exception.Message, null),
                ValidationException validationEx => (HttpStatusCode.BadRequest, validationEx.Message, validationEx.Errors),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.", null)
            };

            httpContext.Response.StatusCode = (int)statusCode;
            httpContext.Response.ContentType = "application/json";

            var response = ApiResponse<object>.FailureResponse(message, errors);

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }
    }
}
