using IOCv2.API.Attributes;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Authentication.Commands.ChangePassword;
using IOCv2.Application.Features.Authentication.Commands.Login;
using IOCv2.Application.Features.Authentication.Commands.RefreshTokens;
using IOCv2.Application.Features.Authentication.Commands.RequestPasswordReset;
using IOCv2.Application.Features.Authentication.Commands.ResetPassword;
using IOCv2.Application.Features.Authentication.Commands.RevokeToken;
using IOCv2.Application.Features.Users.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Auth;

/// <summary>
/// Authentication — login, logout, token refresh, and password management.
/// </summary>
[Tags("Auth")]
public class AuthController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;

    public AuthController(IMediator mediator, IWebHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
    }

    /// <summary>
    /// Authenticate with email and password. Returns access token and sets HTTP-only cookies.
    /// </summary>
    [HttpPost("login")]
    [RateLimit(maxRequests: 5, windowMinutes: 10, blockMinutes: 5)]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsSuccess && result.Data != null)
        {
            SetTokenCookies(result.Data);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Refresh an expired access token using the refresh token stored in the HTTP-only cookie.
    /// </summary>
    [HttpPost("tokens/refresh")]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Logout the current user — revokes refresh token and clears authentication cookies.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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
        var revokeCommand = new RevokeTokenCommand { RefreshToken = refreshToken };
        await _mediator.Send(revokeCommand);

        return NoContent();
    }

    /// <summary>
    /// Change the current user's password. Requires authentication.
    /// </summary>
    [HttpPost("passwords/change")]
    [Authorize]
    [ProducesResponseType(typeof(Result<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Send a password reset email to the specified address.
    /// </summary>
    [HttpPost("passwords/reset-request")]
    [RateLimit(maxRequests: 3, windowMinutes: 10, blockMinutes: 10)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetCommand command)
    {
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Reset the user's password using a valid reset token received via email.
    /// </summary>
    [HttpPost("passwords/reset")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get the currently authenticated user's profile information.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(Result<GetMyProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUserInfo()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid token claims." });
        }

        var result = await _mediator.Send(new GetMyProfileQuery { UserId = userId });
        return HandleResult(result);
    }

    private void SetTokenCookies(LoginResponse response)
    {
        var accessCookieOptions = BuildCookieOptions(response.ExpiresIn);
        var cookieOptions = BuildCookieOptions(response.ExpiresIn);

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
