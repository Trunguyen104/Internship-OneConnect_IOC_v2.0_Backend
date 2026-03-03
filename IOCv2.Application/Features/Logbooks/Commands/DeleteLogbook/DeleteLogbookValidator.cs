using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Commands.DeleteLogbook
{
    public class DeleteLogbookValidator : AbstractValidator<DeleteLogbookCommand>
    {
        public DeleteLogbookValidator()
        {
            RuleFor(x => x.LogbookId)
                .NotEmpty()
                .WithMessage("Logbook ID is required.");
        }
    }
}
