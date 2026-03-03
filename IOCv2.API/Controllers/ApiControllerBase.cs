using IOCv2.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result == null) return BadRequest();

        if (result.IsSuccess)
        {
            if (result.Data == null) return NoContent();

            if (result.HasWarning)
            {
                return Ok(new
                {
                    data = result.Data,
                    warning = result.Warning
                });
            }

            return Ok(new { data = result.Data });
        }

        return StatusCode(ResolveStatusCode(result), new ErrorResponse(ResolveStatusCode(result), result.Error ?? "An error occurred"));
    }

    /// <summary>
    /// Returns HTTP 201 Created on success. Use for POST creation actions.
    /// </summary>
    protected IActionResult HandleCreatedResult<T>(Result<T> result)
    {
        if (result == null) return BadRequest();

        if (result.IsSuccess)
        {
            if (result.Data == null) return NoContent();
            return StatusCode(StatusCodes.Status201Created, new { data = result.Data });
        }

        var code = ResolveStatusCode(result);
        return StatusCode(code, new ErrorResponse(code, result.Error ?? "An error occurred"));
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
