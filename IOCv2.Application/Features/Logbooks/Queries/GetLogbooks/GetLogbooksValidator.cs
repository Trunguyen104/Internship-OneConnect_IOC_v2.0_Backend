using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbooks
{
    public class GetLogbooksValidator : AbstractValidator<GetLogbooksQuery>
    {
        public GetLogbooksValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(100);
        }
    }
}
