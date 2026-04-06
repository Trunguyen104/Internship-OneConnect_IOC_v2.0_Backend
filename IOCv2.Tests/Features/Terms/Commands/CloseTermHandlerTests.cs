using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Terms.Commands.CloseTerm;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IOCv2.Tests.Features.Terms.Commands
{
    public class CloseTermHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<CloseTermHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly CloseTermHandler _handler;

        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _universityId = Guid.NewGuid();

        public CloseTermHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<CloseTermHandler>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCacheService = new Mock<ICacheService>();

            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());
            _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");

            _handler = new CloseTermHandler(
                _mockUnitOfWork.Object,
                _mockMessageService.Object,
                _mockLogger.Object,
                _mockCurrentUserService.Object,
                _mockCacheService.Object);
        }

        private Term CreateActiveTerm(Guid termId)
        {
            return new Term
            {
                TermId = termId,
                UniversityId = _universityId,
                Name = "Active Term",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), // started 10 days ago
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),    // ends in 90 days
                Status = TermStatus.Open,
                Version = 1,
                TotalEnrolled = 0,
                TotalPlaced = 0,
                TotalUnplaced = 0
            };
        }

        [Fact]
        public async Task Handle_TermNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term>().AsQueryable().BuildMock());

            var command = new CloseTermCommand
            {
                TermId = Guid.NewGuid(),
                Version = 1,
                Reason = "No longer needed"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_VersionConflict_ReturnsConflict()
        {
            // Arrange
            var termId = Guid.NewGuid();
            var term = CreateActiveTerm(termId); // Version = 1

            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term> { term }.AsQueryable().BuildMock());

            var command = new CloseTermCommand
            {
                TermId = termId,
                Version = 99, // Wrong version
                Reason = "Closing early"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
        }

        [Fact]
        public async Task Handle_TermNotActive_ReturnsFailure()
        {
            // Arrange — term is Upcoming (StartDate in the future → not Active, not Ended)
            var termId = Guid.NewGuid();
            var term = new Term
            {
                TermId = termId,
                UniversityId = _universityId,
                Name = "Upcoming Term",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
                Status = TermStatus.Open,
                Version = 1
            };

            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term> { term }.AsQueryable().BuildMock());

            var command = new CloseTermCommand
            {
                TermId = termId,
                Version = 1,
                Reason = "Trying to close upcoming term"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            // Handler returns Failure without a specific error type (general failure)
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ValidClose_ReturnsSuccess()
        {
            // Arrange
            var termId = Guid.NewGuid();
            var term = CreateActiveTerm(termId);

            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term> { term }.AsQueryable().BuildMock());

            _mockUnitOfWork.Setup(x => x.Repository<Term>()
                .UpdateAsync(It.IsAny<Term>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var command = new CloseTermCommand
            {
                TermId = termId,
                Version = 1,
                Reason = "Internship period ended"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            term.Status.Should().Be(TermStatus.Closed);
            term.ClosedBy.Should().Be(_userId);
            term.CloseReason.Should().Be("Internship period ended");
            term.Version.Should().Be(2);
            _mockUnitOfWork.Verify(x => x.Repository<Term>().UpdateAsync(term, It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }
    }
}
