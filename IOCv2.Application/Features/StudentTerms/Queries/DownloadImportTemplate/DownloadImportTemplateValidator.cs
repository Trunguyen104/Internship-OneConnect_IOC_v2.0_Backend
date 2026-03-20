using FluentValidation;

namespace IOCv2.Application.Features.StudentTerms.Queries.DownloadImportTemplate;

public class DownloadImportTemplateValidator : AbstractValidator<DownloadImportTemplateQuery>
{
    public DownloadImportTemplateValidator()
    {
        RuleFor(x => x.TermId)
            .NotEmpty().WithMessage("TermId không được để trống");
    }
}
