using Asp.Versioning;
using IOCv2.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result == null) return BadRequest(ApiResponse<object>.Fail("Invalid result"));

        if (result.IsSuccess)
        {
            if (result.Data == null) 
            {
                return Ok(new ApiResponse<object>(true, result.Message ?? "Request successful", new { }));
            }

            return Ok(new ApiResponse<T>(true, result.Message ?? "Request successful", result.Data));
        }

        var code = ResolveStatusCode(result);
        var errors = !string.IsNullOrEmpty(result.Error) ? new List<string> { result.Error } : null;
        var message = result.Error ?? result.Message ?? "An error occurred";
        return StatusCode(code, new ErrorResponse(code, message, errors));
    }

    /// <summary>
    /// Handles resource creation responses (201 Created).
    /// </summary>
    protected IActionResult HandleCreateResult<T>(Result<T> result, string actionName, object routeValues)
    {
        if (result == null) return BadRequest(ApiResponse<object>.Fail("Invalid result"));

        if (result.IsSuccess && result.Data != null)
        {
            return CreatedAtAction(actionName, routeValues, 
                new ApiResponse<T>(true, result.Message ?? "Created successfully", result.Data));
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Safely retrieves the current user's ID from claims.
    /// </summary>
    protected Guid? GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdStr, out var userId) ? userId : null;
    }

    private static int ResolveStatusCode<T>(Result<T> result) => result.ErrorType switch
    {
        ResultErrorType.NotFound    => StatusCodes.Status404NotFound,
        ResultErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ResultErrorType.Forbidden   => StatusCodes.Status403Forbidden,
        ResultErrorType.Conflict    => StatusCodes.Status409Conflict,
        ResultErrorType.InternalServerError => StatusCodes.Status500InternalServerError,
        ResultErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
        _                           => StatusCodes.Status400BadRequest
    };
}
