using FluentValidation;
using IOCv2.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using IOCv2.Application.Common.Models;

namespace IOCv2.API.Middlewares
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
            _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

            // ValidationException — trả về field-keyed errors
            if (exception is ValidationException validationException)
            {
                var validationErrors = validationException.Errors
                    .GroupBy(e => ToCamelCase(e.PropertyName))
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToList());

                var validationResponse = new ErrorResponse(
                    (int)HttpStatusCode.BadRequest,
                    "Validation Error",
                    validationErrors);

                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await httpContext.Response.WriteAsJsonAsync(validationResponse, cancellationToken);
                return true;
            }

            var statusCode = (int)HttpStatusCode.InternalServerError;
            var message = "An unexpected error occurred.";
            List<string>? errors = null;

            _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);
            switch (exception)
            {
                case DomainViolationException domainEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = domainEx.Message;
                    break;
                case BusinessException businessException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = businessException.Message;
                    break;
                case NotFoundException notFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = notFoundException.Message;
                    break;
                case ConflictException conflictException:
                    statusCode = (int)HttpStatusCode.Conflict;
                    message = conflictException.Message;
                    if (!string.IsNullOrEmpty(conflictException.PropertyName))
                    {
                        var prop = ToCamelCase(conflictException.PropertyName);
                        var validationResponse = new ErrorResponse(
                            statusCode,
                            message,
                            new Dictionary<string, List<string>> { { prop, new List<string> { message } } });
                        httpContext.Response.StatusCode = statusCode;
                        await httpContext.Response.WriteAsJsonAsync(validationResponse, cancellationToken);
                        return true;
                    }
                    break;
                case UnauthorizedAccessException unauthorizedEx:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    message = unauthorizedEx.Message;
                    break;
                case TaskCanceledException _:
                    statusCode = (int)HttpStatusCode.RequestTimeout;
                    message = "Request was cancelled.";
                    break;
            }

            var errorResponse = new ErrorResponse(statusCode, message, errors);
            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
            return true;
        }

        private static string ToCamelCase(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return "general";
            return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        }
    }
}
