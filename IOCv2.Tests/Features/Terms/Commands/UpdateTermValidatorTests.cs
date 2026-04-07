using FluentAssertions;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Terms.Commands.UpdateTerm;
using IOCv2.Application.Interfaces;
using Moq;

namespace IOCv2.Tests.Features.Terms.Commands;

public class UpdateTermValidatorTests
{
    private readonly UpdateTermValidator _validator;

    public UpdateTermValidatorTests()
    {
        var messageService = new Mock<IMessageService>();
        messageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        _validator = new UpdateTermValidator(messageService.Object);
    }

    [Fact]
    public void Validate_EndDateOneDayAfterStart_ShouldHaveMinDurationError()
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var command = new UpdateTermCommand
        {
            TermId = Guid.NewGuid(),
            Name = "Term 2026",
            StartDate = startDate,
            EndDate = startDate.AddDays(1),
            Version = 1
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
        var command = new UpdateTermCommand
        {
            TermId = Guid.NewGuid(),
            Name = "Term 2026",
            StartDate = startDate,
            EndDate = startDate.AddMonths(1),
            Version = 1
        };

        var result = _validator.Validate(command);

        result.Errors.Should().NotContain(x =>
            x.ErrorMessage == MessageKeys.Terms.EndDateMustBeAtLeastOneMonthAfterStart);
    }
}

