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
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, IWebHostEnvironment env, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate with email and password. Returns user info and sets HTTP-only cookies for tokens.
    /// </summary>
    /// <param name="command">Login credentials</param>
    /// <returns>User profile and access/refresh cookies</returns>
    [HttpPost("login")]
    [RateLimit(maxRequests: 5, windowMinutes: 10, blockMinutes: 5)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        _logger.LogInformation("Login attempt for email: {Email}", command.Email);
        
        var result = await _mediator.Send(command);

        if (result.IsSuccess && result.Data != null)
        {
            _logger.LogInformation("Login successful for email: {Email}", command.Email);
            SetTokenCookies(result.Data);
        }
        else
        {
            _logger.LogWarning("Login failed for email: {Email}. Reason: {Reason}", command.Email, result.Message);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Refresh an expired access token using the refresh token stored in the HTTP-only cookie.
    /// </summary>
    /// <returns>New access token cookie and user info</returns>
    [HttpPost("tokens/refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken()
    {
        _logger.LogInformation("Token refresh requested.");

        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Refresh token not found in cookies.");
            return Unauthorized(ApiResponse<object>.Fail("Refresh token not found."));
        }

        var command = new RefreshTokenCommand(refreshToken);
        var result = await _mediator.Send(command);

        if (result.IsSuccess && result.Data != null)
        {
            _logger.LogInformation("Token refreshed successfully.");
            SetTokenCookies(result.Data);
        }
        else
        {
            _logger.LogWarning("Token refresh failed. Reason: {Reason}", result.Message);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Logout the current user — revokes refresh token and clears authentication cookies.
    /// </summary>
    /// <param name="command">Optional refresh token for revocation</param>
    /// <returns>NoContent success</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RevokeTokenCommand command)
    {
        _logger.LogInformation("Logout requested.");

        // Try to get token from Body or Cookie
        var refreshToken = command.RefreshToken ?? Request.Cookies["refreshToken"];

        // Clear Cookies to ensure client-side logout
        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");

        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogInformation("No refresh token to revoke. Local cookies cleared.");
            return NoContent();
        }

        // Revoke token in DB
        var revokeCommand = new RevokeTokenCommand { RefreshToken = refreshToken };
        await _mediator.Send(revokeCommand);

        _logger.LogInformation("Logout/Revocation completed.");
        return NoContent();
    }

    /// <summary>
    /// Change the current user's password. Requires authentication.
    /// </summary>
    /// <param name="command">Current and new password</param>
    /// <returns>Success message</returns>
    [HttpPost("passwords/change")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        _logger.LogInformation("Password change requested.");
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Send a password reset email to the specified address.
    /// </summary>
    /// <param name="command">User's email</param>
    /// <returns>Success message</returns>
    [HttpPost("passwords/reset-request")]
    [RateLimit(maxRequests: 3, windowMinutes: 10, blockMinutes: 10)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetCommand command)
    {
        _logger.LogInformation("Password reset request for: {Email}", command.Email);
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Reset the user's password using a valid reset token received via email.
    /// </summary>
    /// <param name="command">Reset token and new password</param>
    /// <returns>Success message</returns>
    [HttpPost("passwords/reset")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        _logger.LogInformation("Password reset execution.");
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get the currently authenticated user's profile information.
    /// </summary>
    /// <returns>User profile response</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetMyProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUserInfo()
    {
        _logger.LogInformation("Fetching current user info.");

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("Unauthorized access: Invalid token claims.");
            return Unauthorized(ApiResponse<object>.Fail("Invalid token claims."));
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
