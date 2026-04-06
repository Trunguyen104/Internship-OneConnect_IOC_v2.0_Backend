using IOCv2.API.Attributes;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Auth;

/// <summary>
/// Public landing page: send and verify email OTP before consultation reservation.
/// </summary>
[Tags("Auth")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/auth/landing")]
public class LandingOtpController : ResultHandlingControllerBase
{
    private readonly IOtpService _otpService;

    public LandingOtpController(IOtpService otpService)
    {
        _otpService = otpService;
    }

    /// <summary>Sends a 6-digit OTP to the given email (rate limited).</summary>
    [HttpPost("send-otp")]
    [RateLimit(maxRequests: 5, windowMinutes: 1, blockMinutes: 5)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SendOtp([FromBody] SendLandingOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await _otpService.SendLandingOtpAsync(request.Email ?? string.Empty, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Verifies the OTP and marks the email as verified for the next reservation submit.</summary>
    [HttpPost("verify-otp")]
    [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 2)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyLandingOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await _otpService.VerifyLandingOtpAsync(
            request.Email ?? string.Empty,
            request.OtpCode ?? string.Empty,
            cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Checks if email is already registered to skip OTP.</summary>
    [HttpPost("check-email")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckEmail([FromBody] SendLandingOtpRequest request, [FromServices] ILandingEmailPolicy landingEmailPolicy, [FromServices] IMessageService messageService, CancellationToken cancellationToken)
    {
        var isRegistered = await landingEmailPolicy.IsRegisteredEmailAsync(request.Email ?? string.Empty, cancellationToken);
        var message = isRegistered
            ? messageService.GetMessage(MessageKeys.Landing.AlreadyRegistered)
            : string.Empty;
        return Ok(new ApiResponse<bool>(true, message, isRegistered));
    }
}

public sealed record SendLandingOtpRequest(string? Email);

public sealed record VerifyLandingOtpRequest(string? Email, string? OtpCode);
