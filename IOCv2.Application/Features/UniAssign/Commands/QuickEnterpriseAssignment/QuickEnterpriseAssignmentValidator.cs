using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.UniAssign.Commands.QuickEnterpriseAssignment
{
    public class QuickEnterpriseAssignmentValidator : AbstractValidator<QuickEnterpriseAssignmentCommand>
    {
        private readonly IMessageService _messageService;

        public QuickEnterpriseAssignmentValidator(IMessageService messageService)
        {
            _messageService = messageService;

            RuleFor(x => x.StudentId)
                .NotEmpty()
                .WithMessage(_messageService.GetMessage(MessageKeys.Common.InvalidRequest));

            RuleFor(x => x.TermId)
                .NotEmpty()
                .WithMessage(_messageService.GetMessage(MessageKeys.Common.InvalidRequest));

            RuleFor(x => x.EnterpriseId)
                .NotEmpty()
                .WithMessage(_messageService.GetMessage(MessageKeys.Common.InvalidRequest));

            RuleFor(x => x.InternPhaseId)
                .NotEmpty()
                .WithMessage(_messageService.GetMessage(MessageKeys.Common.InvalidRequest));
        }
    }
}
