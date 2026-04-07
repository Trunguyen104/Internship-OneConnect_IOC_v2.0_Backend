using Asp.Versioning;
using IOCv2.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

/// <summary>
/// Shared <see cref="Result{T}"/> → IActionResult mapping for API controllers.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
public abstract class ResultHandlingControllerBase : ControllerBase
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

    private static int ResolveStatusCode<T>(Result<T> result) => result.ErrorType switch
    {
        ResultErrorType.NotFound => StatusCodes.Status404NotFound,
        ResultErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ResultErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ResultErrorType.Conflict => StatusCodes.Status409Conflict,
        ResultErrorType.InternalServerError => StatusCodes.Status500InternalServerError,
        ResultErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
        _ => StatusCodes.Status400BadRequest
    };
}
