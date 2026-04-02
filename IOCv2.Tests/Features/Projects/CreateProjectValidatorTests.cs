using FluentAssertions;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Commands.CreateProject;
using IOCv2.Application.Interfaces;
using Moq;
using System;
using Xunit;

namespace IOCv2.Tests.Features.Projects
{
    public class CreateProjectValidatorTests
    {
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly CreateProjectValidator _validator;

        public CreateProjectValidatorTests()
        {
            _mockMessageService = new Mock<IMessageService>();

            // Setup message service to return the key itself so we can verify the exact error constant
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>()))
                .Returns((string key) => key);

            _validator = new CreateProjectValidator(_mockMessageService.Object);
        }

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateProjectCommand
            {
                ProjectName = "Valid Project",
                Description = "A valid project description",
                Field = "IT",
                Requirements = "Project requirements",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Validate_InvalidProjectName_ShouldHaveRequiredError(string? projectName)
        {
            // Arrange
            var command = new CreateProjectCommand
            {
                ProjectName = projectName // Invalid
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(CreateProjectCommand.ProjectName) &&
                e.ErrorMessage == MessageKeys.Projects.ProjectsProjectNameRequired);
        }

        [Fact]
        public void Validate_ProjectNameExceedsMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateProjectCommand
            {
                ProjectName = new string('A', 256) // Exceeds max 255
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(CreateProjectCommand.ProjectName) &&
                e.ErrorMessage == MessageKeys.Projects.ProjectNameMaxLength);
        }

        [Fact]
        public void Validate_DescriptionExceedsMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateProjectCommand
            {
                ProjectName = "Valid Project",
                Description = new string('B', 2001) // Exceeds max 2000
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(CreateProjectCommand.Description) &&
                e.ErrorMessage == MessageKeys.Projects.DescriptionMaxLength);
        }

        [Fact]
        public void Validate_StartDateGreaterThanEndDate_ShouldHaveRangeError()
        {
            // Arrange
            var command = new CreateProjectCommand
            {
                ProjectName = "Valid Project",
                StartDate = DateTime.UtcNow.AddDays(10), // Start after End
                EndDate = DateTime.UtcNow.AddDays(5)
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();

            // Both StartDate and EndDate have rules checking against each other
            result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(CreateProjectCommand.StartDate) &&
                e.ErrorMessage == MessageKeys.Projects.StartDateInvalidRange);

            result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(CreateProjectCommand.EndDate) &&
                e.ErrorMessage == MessageKeys.Projects.EndDateInvalidRange);
        }
    }
}
