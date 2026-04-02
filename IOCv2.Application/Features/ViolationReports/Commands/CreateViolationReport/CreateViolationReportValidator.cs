using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.ViolationReport;
using IOCv2.Application.Interfaces;
using System;

namespace IOCv2.Application.Features.ViolationReports.Commands.CreateViolationReport
{
    public class CreateViolationReportValidator : AbstractValidator<CreateViolationReportCommand>
    {
        private readonly IMessageService _messageService;
        public CreateViolationReportValidator(IMessageService messageService)
        {
            _messageService = messageService;
            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.StudentMessageKey.StudentIdRequired));

            RuleFor(x => x.OccurredDate)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.ViolationReportKey.OccurredDateIsRequired))
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage(_messageService.GetMessage(MessageKeys.ViolationReportKey.OccurredDateInFuture));

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.ViolationReportKey.DescriptionIsRequired))
                .MinimumLength(ViolationReportParam.MinDescriptionLength).WithMessage(_messageService.GetMessage(MessageKeys.ViolationReportKey.DescriptionMinLength, ViolationReportParam.MinDescriptionLength))
                .MaximumLength(2000).WithMessage(_messageService.GetMessage(MessageKeys.ViolationReportKey.DescriptionMaxLength, 2000));
        }
    }
}