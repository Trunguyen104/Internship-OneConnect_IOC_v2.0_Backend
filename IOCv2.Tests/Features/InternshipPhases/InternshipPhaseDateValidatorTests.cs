using FluentAssertions;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipPhases.Commands.CreateInternshipPhase;
using IOCv2.Application.Features.InternshipPhases.Commands.UpdateInternshipPhase;
using IOCv2.Application.Interfaces;
using Moq;

namespace IOCv2.Tests.Features.InternshipPhases;

public class InternshipPhaseDateValidatorTests
{
    private readonly Mock<IMessageService> _mockMessageService = new();

    public InternshipPhaseDateValidatorTests()
    {
        _mockMessageService
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns((string key) => key);
    }

    [Fact]
    public void CreateValidator_StartDateInPast_ShouldFail()
    {
        var validator = new CreateInternshipPhaseValidator(_mockMessageService.Object);
        var command = BuildCreateCommand(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateInternshipPhaseCommand.StartDate) &&
            e.ErrorMessage.StartsWith(MessageKeys.InternshipPhase.StartDateNotInPast));
    }

    [Fact]
    public void CreateValidator_StartDateToday_ShouldPassDateRules()
    {
        var validator = new CreateInternshipPhaseValidator(_mockMessageService.Object);
        var command = BuildCreateCommand(DateOnly.FromDateTime(DateTime.UtcNow));

        var result = validator.Validate(command);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInternshipPhaseCommand.StartDate));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateInternshipPhaseCommand.EndDate));
    }

    [Fact]
    public void UpdateValidator_StartDateInPast_ShouldFail()
    {
        var validator = new UpdateInternshipPhaseValidator(_mockMessageService.Object);
        var command = BuildUpdateCommand(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateInternshipPhaseCommand.StartDate) &&
            e.ErrorMessage.StartsWith(MessageKeys.InternshipPhase.StartDateNotInPast));
    }

    [Fact]
    public void UpdateValidator_StartDateToday_ShouldPassDateRules()
    {
        var validator = new UpdateInternshipPhaseValidator(_mockMessageService.Object);
        var command = BuildUpdateCommand(DateOnly.FromDateTime(DateTime.UtcNow));

        var result = validator.Validate(command);

        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInternshipPhaseCommand.StartDate));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateInternshipPhaseCommand.EndDate));
    }

    private static CreateInternshipPhaseCommand BuildCreateCommand(DateOnly startDate)
    {
        return new CreateInternshipPhaseCommand
        {
            EnterpriseId = Guid.NewGuid(),
            Name = "Spring Internship",
            StartDate = startDate,
            EndDate = startDate.AddDays(30),
            MajorFields = "Software Engineering",
            Capacity = 30,
            Description = "Internship for final-year students"
        };
    }

    private static UpdateInternshipPhaseCommand BuildUpdateCommand(DateOnly startDate)
    {
        return new UpdateInternshipPhaseCommand
        {
            PhaseId = Guid.NewGuid(),
            Name = "Spring Internship",
            StartDate = startDate,
            EndDate = startDate.AddDays(30),
            MajorFields = "Software Engineering",
            Capacity = 30,
            Description = "Internship for final-year students"
        };
    }
}

