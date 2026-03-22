using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Logbooks.Commands.CreateLogbook;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace IOCv2.Tests.Features.Logbooks
{
    public class CreateLogbookHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<CreateLogbookHandler>> _mockLogger;
        private readonly Mock<IGenericRepository<InternshipGroup>> _mockInternshipRepo;
        private readonly Mock<IGenericRepository<Student>> _mockStudentRepo;
        private readonly Mock<IGenericRepository<Logbook>> _mockLogbookRepo;
        private readonly Mock<IGenericRepository<AuditLog>> _mockAuditLogRepo;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly CreateLogbookHandler _handler;

        public CreateLogbookHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<CreateLogbookHandler>>();
            _mockCacheService = new Mock<ICacheService>();

            _mockInternshipRepo = new Mock<IGenericRepository<InternshipGroup>>();
            _mockStudentRepo = new Mock<IGenericRepository<Student>>();
            _mockLogbookRepo = new Mock<IGenericRepository<Logbook>>();
            _mockAuditLogRepo = new Mock<IGenericRepository<AuditLog>>();

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(_mockInternshipRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Student>()).Returns(_mockStudentRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Logbook>()).Returns(_mockLogbookRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<AuditLog>()).Returns(_mockAuditLogRepo.Object);

            _handler = new CreateLogbookHandler(
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
            var userId = Guid.NewGuid().ToString();
            var internshipId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var command = new CreateLogbookCommand 
            { 
                InternshipId = internshipId, 
                Summary = "Test Summary", 
                Plan = "Test Plan", 
                DateReport = DateTime.UtcNow 
            };

            _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
            _mockInternshipRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<InternshipGroup, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            _mockStudentRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Student> { new Student { StudentId = studentId, UserId = Guid.Parse(userId) } });

            _mockMapper.Setup(x => x.Map<CreateLogbookResponse>(It.IsAny<Logbook>()))
                .Returns(new CreateLogbookResponse { Summary = command.Summary });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockLogbookRepo.Verify(x => x.AddAsync(It.IsAny<Logbook>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InternshipNotFound_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var command = new CreateLogbookCommand { InternshipId = Guid.NewGuid(), Summary = "Test", Plan = "Test" };

            _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
            _mockInternshipRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<InternshipGroup, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Internship group not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Internship group not found");
            _mockLogbookRepo.Verify(x => x.AddAsync(It.IsAny<Logbook>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
