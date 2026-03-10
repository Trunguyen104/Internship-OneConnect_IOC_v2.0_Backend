using IOCv2.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result == null) return BadRequest(ApiResponse<object>.Fail("Invalid result"));

        if (result.IsSuccess)
        {
            var message = result.HasWarning ? $"Request successful. Warning: {result.Warning}" : "Request successful";
            
            if (result.Data == null) 
            {
                return Ok(new ApiResponse<object>(true, message, new { }));
            }

            return Ok(new ApiResponse<T>(true, message, result.Data));
        }

        var code = ResolveStatusCode(result);
        return StatusCode(code, ApiResponse<object>.Fail(result.Message ?? result.Error ?? "An error occurred"));
    }

    /// <summary>
    /// Returns HTTP 201 Created on success. Use for POST creation actions.
    /// </summary>
    protected IActionResult HandleCreatedResult<T>(Result<T> result)
    {
        if (result == null) return BadRequest(ApiResponse<object>.Fail("Invalid result"));

        if (result.IsSuccess)
        {
            if (result.Data == null) 
            {
                return StatusCode(StatusCodes.Status201Created, new ApiResponse<object>(true, "Resource created successfully", new { }));
            }
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<T>(true, "Resource created successfully", result.Data));
        }

        var code = ResolveStatusCode(result);
        return StatusCode(code, ApiResponse<object>.Fail(result.Message ?? result.Error ?? "An error occurred"));
    }

    private static int ResolveStatusCode<T>(Result<T> result) => result.ErrorType switch
    {
        ResultErrorType.NotFound    => StatusCodes.Status404NotFound,
        ResultErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ResultErrorType.Forbidden   => StatusCodes.Status403Forbidden,
        ResultErrorType.Conflict    => StatusCodes.Status409Conflict,
        _                           => StatusCodes.Status400BadRequest
    };
}
