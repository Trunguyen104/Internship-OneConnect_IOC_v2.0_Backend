using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using IOCv2.Application.Features.Jobs.Commands.PublishJobPosting;
using IOCv2.Application.Constants;

namespace IOCv2.Tests.Features.Jobs.Commands
{
    public class PublishJobHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Job>> _repoMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<PublishJobHandler>> _loggerMock;
        private readonly Mock<IMessageService> _messageServiceMock;

        public PublishJobHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _repoMock = new Mock<IGenericRepository<Job>>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<PublishJobHandler>>();
            _messageServiceMock = new Mock<IMessageService>();

            // Default unit of work / repo wiring
            _unitOfWorkMock.Setup(u => u.Repository<Job>()).Returns(_repoMock.Object);

            // Transaction methods default to completed tasks
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            // Message service: return key or a simple string for assertions
            _messageServiceMock.Setup(m => m.GetMessage(It.IsAny<string>()))
                .Returns((string key) => key);
            _messageServiceMock.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string key, object[] _) => key);

            // Current user default
            _currentUserServiceMock.SetupGet(c => c.UserId).Returns(Guid.NewGuid().ToString());
        }

        private PublishJobHandler CreateHandler()
            => new PublishJobHandler(
                _unitOfWorkMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object,
                _messageServiceMock.Object);

        [Fact]
        public async Task Handle_ReturnsNotFound_WhenJobNotFound()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Job?)null);

            var handler = CreateHandler();
            var command = new PublishJobCommand { JobId = jobId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task Handle_ReturnsBadRequest_WhenJobAlreadyPublished()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var job = new Job
            {
                JobId = jobId,
                Status = JobStatus.PUBLISHED
                // other properties not needed for this validation scenario
            };

            _repoMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(job);

            var handler = CreateHandler();
            var command = new PublishJobCommand { JobId = jobId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.NotNull(result.Error);
            // message service returns the key string; ensure the returned error contains that key value
            Assert.Contains(MessageKeys.JobPostingMessageKey.AlreadyPublished, result.Error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Handle_ReturnsBadRequest_WhenTitleMissing()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var job = new Job
            {
                JobId = jobId,
                Status = JobStatus.DRAFT,
                Title = null // missing title triggers validation error
            };

            _repoMock.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(job);

            var handler = CreateHandler();
            var command = new PublishJobCommand { JobId = jobId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            Assert.NotNull(result.Error);
            Assert.Contains("TitleRequired", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }
}