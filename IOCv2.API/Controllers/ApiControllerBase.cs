using IOCv2.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ResultHandlingControllerBase
{
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
}
