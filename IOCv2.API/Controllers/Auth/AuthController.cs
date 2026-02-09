using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Authentication.Commands.Login;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IWebHostEnvironment _env;

        public AuthController(IMediator mediator, IWebHostEnvironment env)
        {
            _mediator = mediator;
            _env = env;
        }

        private IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            return result.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(new { message = result.Errors }),
                ResultErrorType.Unauthorized => Unauthorized(new { message = result.Errors }),
                ResultErrorType.Forbidden => Forbid(),
                _ => BadRequest(new { message = result.Errors }),
            };
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Data != null)
            {
                SetTokenCookies(result.Data);
            }

            return HandleResult(result);
        }

        //[HttpPost("refresh-token")]
        //public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        //{
        //    var result = await _mediator.Send(command);

        //    if (result.IsSuccess && result.Data != null)
        //    {
        //        SetTokenCookies(result.Data);
        //    }

        //    return HandleResult(result);
        //}
        private void SetTokenCookies(LoginResponse response)
        {
            var isDev = _env.IsDevelopment();

            var cookieOptions = BuildCookieOptions(response.ExpiresIn);

            var accessCookieOptions = BuildCookieOptions(response.ExpiresIn);

            Response.Cookies.Append("accessToken", response.AccessToken, accessCookieOptions);
            Response.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions);
        }

        private CookieOptions BuildCookieOptions(int expiresIn)
        {
            var isDev = _env.IsDevelopment();

            return new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddSeconds(expiresIn),
                Secure = !isDev,
                SameSite = isDev ? SameSiteMode.Unspecified : SameSiteMode.None,
                Path = "/"
            };
        }

    }
}
