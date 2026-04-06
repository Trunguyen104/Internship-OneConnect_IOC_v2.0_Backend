using IOCv2.Application.Common.Models;

namespace IOCv2.Application.Interfaces;

/// <summary>
/// Landing page email OTP: generate, cache, verify, and short-lived "email verified" flag for reservation submit.
/// </summary>
public interface IOtpService
{
    Task<Result<bool>> SendLandingOtpAsync(string email, CancellationToken cancellationToken = default);

    Task<Result<bool>> VerifyLandingOtpAsync(string email, string otpCode, CancellationToken cancellationToken = default);

    Task<bool> IsLandingEmailVerifiedAsync(string email, CancellationToken cancellationToken = default);

    Task ConsumeLandingEmailVerificationAsync(string email, CancellationToken cancellationToken = default);
}
