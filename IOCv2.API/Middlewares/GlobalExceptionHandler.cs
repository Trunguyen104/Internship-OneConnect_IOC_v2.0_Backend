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
            var statusCode = (int)HttpStatusCode.InternalServerError;
            var message = "An unexpected error occurred.";
            List<string>? errors = null;
            
            _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

            switch (exception)
            {
                case ValidationException validationException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = "Validation Error";
                    errors = validationException.Errors.Select(x => x.ErrorMessage).ToList();
                    break;
                case BusinessException businessException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = businessException.Message;
                    break;
                case NotFoundException notFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = notFoundException.Message;
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
    }
}
