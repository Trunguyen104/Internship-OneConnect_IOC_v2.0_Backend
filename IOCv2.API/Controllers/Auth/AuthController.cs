using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Authentication.Commands.ChangePassword;
using IOCv2.Application.Features.Authentication.Commands.Login;
using IOCv2.Application.Features.Authentication.Commands.Logout;
using IOCv2.Application.Features.Authentication.Commands.RefreshTokens;
using IOCv2.Application.Features.Authentication.Commands.RequestPasswordReset;
using IOCv2.Application.Features.Authentication.Commands.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { message = "Refresh token not found." });
            }


            var command = new RefreshTokenCommand(refreshToken);

            var result = await _mediator.Send(command);

            if (result.IsSuccess && result.Data != null)
            {
                SetTokenCookies(result.Data);
            }
            return HandleResult(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RevokeTokenCommand command)
        {
            // Try to get token from Body or Cookie
            var refreshToken = command.RefreshToken ?? Request.Cookies["refreshToken"];

            // Clear Cookies to ensure client-side logout
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");

            if (string.IsNullOrEmpty(refreshToken))
            {
                return NoContent();
            }

            // Revoke token in DB
            // Create a new command with the found token if the original one was empty
            var revokeCommand = new RevokeTokenCommand { RefreshToken = refreshToken };
            await _mediator.Send(revokeCommand);

            return NoContent();

        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        //[HttpGet("me")]
        //[Authorize]
        //public async Task<IActionResult> GetCurrentUserInfo()
        //{
        //    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        //    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        //    {
        //        return Unauthorized(new { message = "Invalid token claims." });
        //    }

        //    var result = await _mediator.Send(new Application.Features.Employees.Queries.GetMyProfile.Query(userId));
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
