using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace IOCv2.Infrastructure.Services;

public class OtpService : IOtpService
{
    private const int OtpLength = 6;
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan VerifiedTtl = TimeSpan.FromMinutes(15);

    private readonly ICacheService _cache;
    private readonly IEmailService _emailService;
    private readonly IMessageService _messageService;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        ICacheService cache,
        IEmailService emailService,
        IMessageService messageService,
        ILogger<OtpService> logger)
    {
        _cache = cache;
        _emailService = emailService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<bool>> SendLandingOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        if (normalized == null)
        {
            return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Landing.EmailInvalid));
        }

        if (!_emailService.VerifyEmailMxRecordSync(normalized))
        {
            return Result<bool>.Failure(
                _messageService.GetMessage(MessageKeys.Landing.DomainValidationFailed),
                ResultErrorType.BadRequest);
        }

        var otp = GenerateOtp();
        var otpKey = OtpCacheKey(normalized);

        await _cache.SetAsync(otpKey, otp, OtpTtl, cancellationToken);

        var sent = await _emailService.SendVerificationOtpEmailAsync(normalized, otp, cancellationToken);
        if (!sent)
        {
            await _cache.RemoveAsync(otpKey, cancellationToken);
            return Result<bool>.Failure(
                _messageService.GetMessage(MessageKeys.Landing.EmailSentFailed),
                ResultErrorType.InternalServerError);
        }

        return Result<bool>.Success(true, _messageService.GetMessage(MessageKeys.Landing.OtpSentLabel));
    }

    public async Task<Result<bool>> VerifyLandingOtpAsync(string email, string otpCode, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        if (normalized == null)
        {
            return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Landing.EmailInvalid));
        }

        if (string.IsNullOrWhiteSpace(otpCode) || otpCode.Length != OtpLength || !otpCode.All(char.IsDigit))
        {
            return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Landing.OtpInvalidFormat));
        }

        var otpKey = OtpCacheKey(normalized);
        var stored = await _cache.GetAsync<string>(otpKey, cancellationToken);
        if (string.IsNullOrEmpty(stored))
        {
            return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Landing.OtpExpired));
        }

        if (!FixedTimeEqualsOtp(stored, otpCode.Trim()))
        {
            return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Landing.OtpIncorrect));
        }

        await _cache.RemoveAsync(otpKey, cancellationToken);
        await _cache.SetAsync(VerifiedCacheKey(normalized), true, VerifiedTtl, cancellationToken);

        return Result<bool>.Success(true, _messageService.GetMessage(MessageKeys.Landing.OtpVerifiedLabel));
    }

    public Task<bool> IsLandingEmailVerifiedAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        if (normalized == null)
        {
            return Task.FromResult(false);
        }

        return _cache.ExistsAsync(VerifiedCacheKey(normalized), cancellationToken);
    }

    public async Task ConsumeLandingEmailVerificationAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        if (normalized == null)
        {
            return;
        }

        await _cache.RemoveAsync(VerifiedCacheKey(normalized), cancellationToken);
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim();
        try
        {
            var addr = new MailAddress(trimmed);
            return addr.Address.ToLowerInvariant();
        }
        catch
        {
            return null;
        }
    }

    private static string OtpCacheKey(string normalizedEmail) => $"landing:otp:{normalizedEmail}";

    private static string VerifiedCacheKey(string normalizedEmail) => $"landing:verified:{normalizedEmail}";

    private static string GenerateOtp() => Random.Shared.Next(1_000_000).ToString("D6", CultureInfo.InvariantCulture);

    private static bool FixedTimeEqualsOtp(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
