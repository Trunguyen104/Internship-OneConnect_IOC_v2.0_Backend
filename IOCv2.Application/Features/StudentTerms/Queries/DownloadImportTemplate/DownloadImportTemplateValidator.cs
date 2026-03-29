using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Queries.DownloadImportTemplate;

public class DownloadImportTemplateValidator : AbstractValidator<DownloadImportTemplateQuery>
{
    public DownloadImportTemplateValidator(IMessageService messageService)
    {
        RuleFor(x => x.TermId)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.StudentTerms.TermIdRequired));
    }
}
