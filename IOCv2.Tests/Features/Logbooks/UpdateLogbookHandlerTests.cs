using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace IOCv2.Tests.Features.Logbooks
{
    public class UpdateLogbookHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<UpdateLogbookHandler>> _mockLogger;
        private readonly Mock<IGenericRepository<Student>> _mockStudentRepo;
        private readonly Mock<IGenericRepository<Logbook>> _mockLogbookRepo;
        private readonly Mock<IGenericRepository<AuditLog>> _mockAuditLogRepo;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly UpdateLogbookHandler _handler;

        public UpdateLogbookHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<UpdateLogbookHandler>>();
            _mockCacheService = new Mock<ICacheService>();

            _mockStudentRepo = new Mock<IGenericRepository<Student>>();
            _mockLogbookRepo = new Mock<IGenericRepository<Logbook>>();
            _mockAuditLogRepo = new Mock<IGenericRepository<AuditLog>>();

            _mockUnitOfWork.Setup(x => x.Repository<Student>()).Returns(_mockStudentRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Logbook>()).Returns(_mockLogbookRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<AuditLog>()).Returns(_mockAuditLogRepo.Object);

            _handler = new UpdateLogbookHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockLogger.Object,
                _mockCacheService.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var logbookId = Guid.NewGuid();
            var command = new UpdateLogbookCommand 
            { 
                LogbookId = logbookId, 
                Summary = "Updated Summary", 
                Plan = "Updated Plan", 
                DateReport = DateTime.UtcNow 
            };

            var logbook = Logbook.Create(Guid.NewGuid(), studentId, "Old Summary", "Old Issue", "Old Plan", DateTime.UtcNow.AddDays(-1));

            _mockCurrentUserService.Setup(x => x.UserId).Returns(userId.ToString());
            _mockLogbookRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Logbook, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Logbook> { logbook });
            
            _mockStudentRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Student> { new Student { StudentId = studentId, UserId = userId } });

            _mockMapper.Setup(x => x.Map<UpdateLogbookResponse>(It.IsAny<Logbook>()))
                .Returns(new UpdateLogbookResponse { LogbookId = logbook.LogbookId, Summary = command.Summary });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockLogbookRepo.Verify(x => x.UpdateAsync(It.IsAny<Logbook>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_LogbookNotFound_ShouldReturnFailure()
        {
            // Arrange
            var command = new UpdateLogbookCommand { LogbookId = Guid.NewGuid(), Summary = "Updated", Plan = "Updated" };

            _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
            _mockLogbookRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Logbook, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Logbook>());
            
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Logbook not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Logbook not found");
            _mockLogbookRepo.Verify(x => x.UpdateAsync(It.IsAny<Logbook>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
