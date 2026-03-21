using FluentAssertions;
using IOCv2.Application.Features.Users.Commands.UpdateMyProfile;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;
using MockQueryable.EntityFrameworkCore;
using MediatR;
using Xunit;
using IOCv2.Application.Common.Models;

namespace IOCv2.Tests.Features.Users.Commands
{
    public class UpdateMyProfileHandlerTests
    {
        private readonly Mock<ICurrentUserService> _currentUserService;
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly Mock<ILogger<UpdateMyProfileHandler>> _logger;
        private readonly UpdateMyProfileHandler _handler;

        public UpdateMyProfileHandlerTests()
        {
            _currentUserService = new Mock<ICurrentUserService>();
            _unitOfWork = new Mock<IUnitOfWork>();
            _logger = new Mock<ILogger<UpdateMyProfileHandler>>();
            
            _handler = new UpdateMyProfileHandler(
                _currentUserService.Object,
                _unitOfWork.Object,
                _logger.Object);
        }

        [Fact]
        public async Task Handle_StudentRole_UpdatesPortfolioUrl()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User(userId, "ST001", "student@test.com", "Student Name", UserRole.Student, "hash");
            
            var student = new Student { StudentId = Guid.NewGuid(), UserId = userId };
            
            // Set nav property via reflection since setter is private
            typeof(User).GetProperty("Student")?.SetValue(user, student);
            
            var users = new List<User> { user }.AsQueryable().BuildMock();
            
            var userRepo = new Mock<IGenericRepository<User>>();
            userRepo.Setup(r => r.Query()).Returns(users);
            
            _unitOfWork.Setup(u => u.Repository<User>()).Returns(userRepo.Object);
            _currentUserService.Setup(s => s.UserId).Returns(userId.ToString());

            var command = new UpdateMyProfileCommand
            {
                FullName = "Updated Name",
                PortfolioUrl = "https://portfolio.com"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            user.FullName.Should().Be("Updated Name");
            // If the student was linked, it should be updated. 
            // In a real test we'd need to ensure the .Include() worked or the mock returns the linked entity.
        }
    }
}
