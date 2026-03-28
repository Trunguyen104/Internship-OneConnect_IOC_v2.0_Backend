using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using IOCv2.Application.Features.Jobs.Commands.CreateJobDraft;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Tests.Features.Jobs.Commands
{
    public class CreateJobDraftHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IGenericRepository<Job>> _jobRepoMock;
        private readonly Mock<IGenericRepository<University>> _univRepoMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<CreateJobDraftHandler>> _loggerMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<IMessageService> _messageMock;

        public CreateJobDraftHandlerTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _jobRepoMock = new Mock<IGenericRepository<Job>>();
            _univRepoMock = new Mock<IGenericRepository<University>>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<CreateJobDraftHandler>>();
            _cacheMock = new Mock<ICacheService>();
            _messageMock = new Mock<IMessageService>();

            _uowMock.Setup(u => u.Repository<Job>()).Returns(_jobRepoMock.Object);
            _uowMock.Setup(u => u.Repository<University>()).Returns(_univRepoMock.Object);

            _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Message service returns the key for easy assertions
            _messageMock.Setup(m => m.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            _messageMock.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns((string key, object[] _) => key);
        }

        private CreateJobDraftHandler CreateHandler()
            => new CreateJobDraftHandler(
                _uowMock.Object,
                _currentUserMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _cacheMock.Object,
                _messageMock.Object);

        [Fact]
        public async Task Handle_ReturnsForbidden_WhenUserNotAssociatedWithEnterprise()
        {
            // Arrange: UnitId missing/invalid
            _currentUserMock.SetupGet(c => c.UnitId).Returns((string?)null);

            var handler = CreateHandler();
            var cmd = new CreateJobDraftCommand
            {
                Title = "Title"
            };

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.Forbidden, result.ErrorType);
            Assert.Equal(MessageKeys.Enterprise.HRNotAssociatedWithEnterprise, result.Error);
        }

        [Fact]
        public async Task Handle_ReturnsBadRequest_WhenTitleMissing()
        {
            // Arrange: valid unit id but missing title
            var enterpriseId = Guid.NewGuid();
            _currentUserMock.SetupGet(c => c.UnitId).Returns(enterpriseId.ToString());

            var handler = CreateHandler();
            var cmd = new CreateJobDraftCommand
            {
                Title = null
            };

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
            // Note: code calls GetMessage twice inside; message mock returns the key
            Assert.Equal(MessageKeys.JobPostingMessageKey.TitleRequired, result.Error);
        }

        [Fact]
        public async Task Handle_ReturnsNotFound_WhenTargetedAndUniversityMissing()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var uniId = Guid.NewGuid();
            _currentUserMock.SetupGet(c => c.UnitId).Returns(enterpriseId.ToString());

            _univRepoMock.Setup(r => r.GetByIdAsync(uniId, It.IsAny<CancellationToken>())).ReturnsAsync((University?)null);

            var handler = CreateHandler();
            var cmd = new CreateJobDraftCommand
            {
                Title = "T",
                Audience = JobAudience.Targeted,
                UniversityId = uniId
            };

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Equal(MessageKeys.University.NotFound, result.Error);
        }

        [Fact]
        public async Task Handle_ReturnsInternalServerError_WhenSaveThrows()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserMock.SetupGet(c => c.UnitId).Returns(enterpriseId.ToString());

            _jobRepoMock.Setup(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Job j, CancellationToken _) => j);

            // Simulate save throws
            _uowMock.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));

            var handler = CreateHandler();
            var cmd = new CreateJobDraftCommand
            {
                Title = "T"
            };

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.InternalServerError, result.ErrorType);
            Assert.Equal(MessageKeys.Common.DatabaseUpdateError, result.Error);
            _uowMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_SavesDraftAndReturnsSuccess_WhenValid()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            _currentUserMock.SetupGet(c => c.UnitId).Returns(enterpriseId.ToString());

            Job? capturedJob = null;
            _jobRepoMock.Setup(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
                .Callback<Job, CancellationToken>((j, _) => capturedJob = j)
                .ReturnsAsync((Job j, CancellationToken _) => j);

            _uowMock.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var mappedResponse = new CreateJobDraftResponse { JobId = Guid.NewGuid(), Title = "T", EnterpriseId = enterpriseId };
            _mapperMock.Setup(m => m.Map<CreateJobDraftResponse>(It.IsAny<Job>())).Returns(mappedResponse);

            var handler = CreateHandler();
            var cmd = new CreateJobDraftCommand
            {
                Title = "T",
                Position = "P",
                Location = "L",
                Audience = JobAudience.Public
            };

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(MessageKeys.JobPostingMessageKey.DraftSavedSuccess, result.Message);
            Assert.NotNull(capturedJob);
            Assert.Equal(JobStatus.DRAFT, capturedJob!.Status);
            _uowMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}