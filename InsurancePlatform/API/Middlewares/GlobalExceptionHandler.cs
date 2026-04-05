using Application.Common.Exceptions;
using Application.Common.Models;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace API.Middlewares
{
    /// <summary>
    /// This class is like a "Safety Net" for the entire application.
    /// If any part of the code crashes or has an error, this class catches it
    /// and sends a nice, clean message back to the user instead of a messy error page.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This method is automatically called whenever an error occurs in the application.
        /// It looks at what kind of error happened and decides what response to send back.
        /// </summary>
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // First, we log the error in our internal system so developers can see what went wrong later.
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            // Here, we check the TYPE of the error (Exception) and decide on a status code and message.
            var (statusCode, message, errors) = exception switch
            {
                // If something was not found (like a missing policy)
                NotFoundException => (HttpStatusCode.NotFound, exception.Message, null),
                
                // If the user sent bad or incorrect data
                BadRequestException => (HttpStatusCode.BadRequest, exception.Message, null),
                
                // If the user is trying to do something they aren't allowed to do
                UnauthorizedException => (HttpStatusCode.Unauthorized, exception.Message, null),
                
                // If the user's data failed specific validation rules (like a missing email address)
                ValidationException validationEx => (HttpStatusCode.BadRequest, validationEx.Message, validationEx.Errors),
                
                // If it's a generic error we didn't expect, we call it an "Internal Server Error"
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.", null)
            };

            // Set the HTTP response status (like 404 for Not Found or 500 for Server Error)
            httpContext.Response.StatusCode = (int)statusCode;
            
            // Tell the browser we are sending back JSON data
            httpContext.Response.ContentType = "application/json";

            // Create a standardized "Failure Response" to send back to the user.
            // This ensures all errors from our API look the same way.
            var response = ApiResponse<object>.FailureResponse(message, errors);

            // Write the JSON response back to the user's browser or app.
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            // Return true to tell .NET that we have handled the error and it doesn't need to do anything else.
            return true;
        }
    }
}
