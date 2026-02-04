using FluentValidation;
using IOCv2.Application.Extensions.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Validators
{
    public class PaginationParamsValidator : AbstractValidator<PaginationParams>
    {
        public PaginationParamsValidator()
        {
            RuleFor(x => x.PageIndex).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(5, 50);
        }
    }
}
