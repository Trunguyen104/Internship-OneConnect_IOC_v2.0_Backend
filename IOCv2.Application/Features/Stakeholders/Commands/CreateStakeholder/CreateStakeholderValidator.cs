﻿using FluentValidation;

namespace IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder
{
    internal class CreateStakeholderValidator : AbstractValidator<CreateStakeholderCommand>
    {
        public CreateStakeholderValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(200)
                .WithMessage("Name cannot exceed 200 characters.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Email format is invalid.")
                .MaximumLength(150)
                .WithMessage("Email cannot exceed 150 characters.");

            RuleFor(x => x.Role)
                .MaximumLength(100)
                .WithMessage("Role cannot exceed 100 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Role));

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^[\+]?[(]?[0-9]{1,4}[)]?[-\s\.]?[(]?[0-9]{1,4}[)]?[-\s\.]?[0-9]{1,9}$")
                .WithMessage("Phone number format is invalid.")
                .MaximumLength(15)
                .WithMessage("Phone number cannot exceed 15 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        }
    }
}

