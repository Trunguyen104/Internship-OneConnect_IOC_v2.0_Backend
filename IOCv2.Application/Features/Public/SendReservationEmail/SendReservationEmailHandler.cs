using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Public.SendReservationEmail
{
    public class SendReservationEmailHandler : IRequestHandler<SendReservationEmailCommand, Result<bool>>
    {
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;
        private readonly ILandingEmailPolicy _landingEmailPolicy;
        private readonly IMessageService _messageService;
        private readonly ILogger<SendReservationEmailHandler> _logger;

        public SendReservationEmailHandler(
            IEmailService emailService,
            IOtpService otpService,
            ILandingEmailPolicy landingEmailPolicy,
            IMessageService messageService,
            ILogger<SendReservationEmailHandler> logger)
        {
            _emailService = emailService;
            _otpService = otpService;
            _landingEmailPolicy = landingEmailPolicy;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(SendReservationEmailCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing landing page reservation for {PartnerName}", request.PartnerName);

            // 1. Kiểm tra chính sách bỏ qua OTP cho user đã đăng ký
            var isRegistered = await _landingEmailPolicy.IsRegisteredEmailAsync(request.Email, cancellationToken);
            if (!isRegistered)
            {
                // 2. Không thuộc diện bỏ qua -> Bắt buộc phải xác thực mã
                var isVerified = await _otpService.IsLandingEmailVerifiedAsync(request.Email, cancellationToken);
                if (!isVerified)
                {
                    return Result<bool>.Failure(
                        _messageService.GetMessage(MessageKeys.Landing.OtpRequired),
                        ResultErrorType.Forbidden);
                }
            }

            var success = await _emailService.SendLandingReservationEmailAsync(
                request.PartnerType,
                request.PartnerName,
                request.Email,
                request.Phone,
                request.Area,
                request.HiringCount,
                request.ConsultationDate,
                request.SelectedTime,
                request.Note ?? string.Empty,
                cancellationToken);

            if (success)
            {
                // Tiêu thụ xác thực (nếu không phải là user đã đăng ký)
                if (!isRegistered)
                {
                    await _otpService.ConsumeLandingEmailVerificationAsync(request.Email, cancellationToken);
                }
                
                return Result<bool>.Success(true, _messageService.GetMessage(MessageKeys.Landing.SendSuccess));
            }

            return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Landing.SendError));
        }
    }
}
