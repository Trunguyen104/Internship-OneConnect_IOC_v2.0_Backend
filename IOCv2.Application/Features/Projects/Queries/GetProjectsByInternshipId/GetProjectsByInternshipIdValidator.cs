using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectsByInternshipId
{
    public class GetProjectsByInternshipIdValidator : AbstractValidator<GetProjectsByInternshipIdQuery>
    {
        private readonly IMessageService _messageService;
        public GetProjectsByInternshipIdValidator(IMessageService messageService)
        {
            _messageService = messageService;
            RuleFor(x => x.InternshipId)
                .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Internships.InternshipIdRequired));
        }
    }
}
