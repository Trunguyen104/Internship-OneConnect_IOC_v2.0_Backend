using System.Net;
using System.Text.Json;
using IOC.Application.Commons.Interfaces.Services;
using IOC.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace IOC.API.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;


    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (DomainException ex)
        {
            await WriteErrorAsync(
                context,
                HttpStatusCode.BadRequest,
                "DOMAIN_ERROR",
                ex.Message);

        }
        catch (ArgumentException ex) {
            await WriteErrorAsync(
                context,
                HttpStatusCode.BadRequest,
                "BAD_REQUEST",
                ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteErrorAsync(
                context,
                HttpStatusCode.Unauthorized,
                "UNAUTHORIZED",
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            await WriteErrorAsync(
                context,
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "Đã xảy ra lỗi hệ thống");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string code,
        string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            code,
            message
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}
