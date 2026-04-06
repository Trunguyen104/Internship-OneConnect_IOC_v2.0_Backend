using FluentAssertions;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder;
using IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder;

namespace IOCv2.Tests.Features.Stakeholders;

public class StakeholderValidatorsTests
{
    [Fact]
    public void CreateStakeholderValidator_ShouldReturnPhoneValidationError_WhenPhoneIsInvalid()
    {
        var validator = new CreateStakeholderValidator();
        var command = new CreateStakeholderCommand
        {
            InternshipId = Guid.NewGuid(),
            Name = "Stakeholder A",
            Email = "valid@email.com",
            PhoneNumber = "99999999999999999999"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateStakeholderCommand.PhoneNumber)
            && e.ErrorMessage == MessageKeys.Stakeholder.PhoneNumberInvalid);
    }

    [Fact]
    public void UpdateStakeholderValidator_ShouldReturnPhoneValidationError_WhenPhoneIsInvalid()
    {
        var validator = new UpdateStakeholderValidator();
        var command = new UpdateStakeholderCommand
        {
            StakeholderId = Guid.NewGuid(),
            InternshipId = Guid.NewGuid(),
            PhoneNumber = "abc-not-a-phone"
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateStakeholderCommand.PhoneNumber)
            && e.ErrorMessage == MessageKeys.Stakeholder.PhoneNumberInvalid);
    }
}

