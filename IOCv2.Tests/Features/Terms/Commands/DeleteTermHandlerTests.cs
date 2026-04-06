using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Terms.Commands.DeleteTerm;
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
    public class DeleteTermHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<DeleteTermHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly DeleteTermHandler _handler;

        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _universityId = Guid.NewGuid();

        public DeleteTermHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<DeleteTermHandler>>();
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

            _handler = new DeleteTermHandler(
                _mockUnitOfWork.Object,
                _mockMessageService.Object,
                _mockLogger.Object,
                _mockCurrentUserService.Object,
                _mockCacheService.Object);
        }

        private Term CreateUpcomingTerm(Guid termId)
        {
            return new Term
            {
                TermId = termId,
                UniversityId = _universityId,
                Name = "Term 2025",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
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
            // Setup Query() returning empty list — simulates IgnoreQueryFilters() returning nothing
            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term>().AsQueryable().BuildMock());

            var command = new DeleteTermCommand(Guid.NewGuid());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_AlreadyDeleted_ReturnsConflict()
        {
            // Arrange
            var termId = Guid.NewGuid();
            var term = CreateUpcomingTerm(termId);
            term.DeletedAt = DateTime.UtcNow.AddDays(-1); // already soft-deleted

            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term> { term }.AsQueryable().BuildMock());

            var command = new DeleteTermCommand(termId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
        }

        [Fact]
        public async Task Handle_TermNotUpcoming_ReturnsFailure()
        {
            // Arrange — term is Active (StartDate in the past, EndDate in the future)
            var termId = Guid.NewGuid();
            var term = new Term
            {
                TermId = termId,
                UniversityId = _universityId,
                Name = "Active Term",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), // started already
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),
                Status = TermStatus.Open,
                Version = 1
            };

            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term> { term }.AsQueryable().BuildMock());

            var command = new DeleteTermCommand(termId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            // Handler returns Failure without an explicit error type (defaults to None / general failure)
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_SuperAdmin_ValidDelete_NoStudents_ReturnsSuccess()
        {
            // Arrange
            var termId = Guid.NewGuid();
            var term = CreateUpcomingTerm(termId);

            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term> { term }.AsQueryable().BuildMock());

            _mockUnitOfWork.Setup(x => x.Repository<Term>()
                .DeleteAsync(It.IsAny<Term>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // No StudentTerms
            _mockUnitOfWork.Setup(x => x.Repository<StudentTerm>().Query())
                .Returns(new List<StudentTerm>().AsQueryable().BuildMock());

            var command = new DeleteTermCommand(termId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.HasRelatedData.Should().BeFalse();
            result.Data.RelatedStudentTermsCount.Should().Be(0);
            _mockUnitOfWork.Verify(x => x.Repository<Term>().DeleteAsync(term, It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
