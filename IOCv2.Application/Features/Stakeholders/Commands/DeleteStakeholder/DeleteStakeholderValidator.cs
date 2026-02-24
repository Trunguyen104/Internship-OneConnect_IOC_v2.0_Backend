﻿using FluentValidation;

namespace IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder
{
    internal class DeleteStakeholderValidator : AbstractValidator<DeleteStakeholderCommand>
    {
        public DeleteStakeholderValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Stakeholder ID is required.");
        }
    }
}

