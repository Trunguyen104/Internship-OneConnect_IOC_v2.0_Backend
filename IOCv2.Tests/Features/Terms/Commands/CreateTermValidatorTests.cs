using FluentAssertions;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Terms.Commands.CreateTerm;
using IOCv2.Application.Interfaces;
using Moq;

namespace IOCv2.Tests.Features.Terms.Commands;

public class CreateTermValidatorTests
{
    private readonly CreateTermValidator _validator;

    public CreateTermValidatorTests()
    {
        var messageService = new Mock<IMessageService>();
        messageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        _validator = new CreateTermValidator(messageService.Object);
    }

    [Fact]
    public void Validate_EndDateOneDayAfterStart_ShouldHaveMinDurationError()
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var command = new CreateTermCommand
        {
            Name = "Term 2026",
            StartDate = startDate,
            EndDate = startDate.AddDays(1),
            UniversityId = Guid.NewGuid()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.ErrorMessage == MessageKeys.Terms.EndDateMustBeAtLeastOneMonthAfterStart);
    }

    [Fact]
    public void Validate_EndDateExactlyOneMonthAfterStart_ShouldBeValid()
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var command = new CreateTermCommand
        {
            Name = "Term 2026",
            StartDate = startDate,
            EndDate = startDate.AddMonths(1),
            UniversityId = Guid.NewGuid()
        };

        var result = _validator.Validate(command);

        result.Errors.Should().NotContain(x =>
            x.ErrorMessage == MessageKeys.Terms.EndDateMustBeAtLeastOneMonthAfterStart);
    }
}

