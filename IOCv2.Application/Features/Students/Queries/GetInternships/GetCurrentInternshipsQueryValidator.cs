using FluentValidation;

namespace IOCv2.Application.Features.Students.Queries.GetInternships
{
    public class GetCurrentInternshipsQueryValidator : AbstractValidator<GetCurrentInternshipsQuery>
    {
        public GetCurrentInternshipsQueryValidator()
        {
            // No parameters to validate for this query
        }
    }
}
