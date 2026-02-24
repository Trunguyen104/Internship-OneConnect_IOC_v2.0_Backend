using IOCv2.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers
{
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

            var statusCode = result.ErrorType switch
            {
                ResultErrorType.NotFound => 404,
                ResultErrorType.Unauthorized => 401,
                ResultErrorType.Forbidden => 403,
                ResultErrorType.Conflict => 409,
                _ => 400
            };

            var response = new ErrorResponse(statusCode, result.Error ?? "An error occurred");
            return StatusCode(statusCode, response);
        }
    }
}
