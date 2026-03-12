using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Terms.Queries.GetTermById;

public class GetTermByIdValidator : AbstractValidator<GetTermByIdQuery>
{
    public GetTermByIdValidator(IMessageService messageService)
    {
        RuleFor(x => x.TermId)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Common.InvalidRequest));
    }
}