using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Commands.UpdateViolationReport
{
    public class UpdateViolationReportValidator : AbstractValidator<UpdateViolationReportCommand>
    {
        private readonly IMessageService _messageService;
        public UpdateViolationReportValidator(IMessageService messageService)
        {
            _messageService = messageService;
            RuleFor(x => x.ViolationReportId)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.ViolationReportKey.ViolationReportIdIsRequired));

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.ViolationReportKey.DescriptionIsRequired))
                .MaximumLength(2000).WithMessage(_messageService.GetMessage(MessageKeys.ViolationReportKey.DescriptionMaxLength, 2000));

            RuleFor(x => x.OccurredDate)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.ViolationReportKey.OccurredDateIsRequired))
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage(_messageService.GetMessage(MessageKeys.ViolationReportKey.OccurredDateInFuture));
        }
    }
}
