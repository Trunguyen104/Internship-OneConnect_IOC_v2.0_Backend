using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Terms.Commands.UpdateTerm;
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
    public class UpdateTermHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<UpdateTermHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly UpdateTermHandler _handler;

        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _universityId = Guid.NewGuid();

        public UpdateTermHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<UpdateTermHandler>>();
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

            _handler = new UpdateTermHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
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
                Name = "Old Term",
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
        public async Task Handle_SuperAdmin_TermNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term>().AsQueryable().BuildMock());

            var command = new UpdateTermCommand
            {
                TermId = Guid.NewGuid(),
                Name = "Updated Term",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
                Version = 1
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
            var term = CreateUpcomingTerm(termId); // Version = 1

            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term> { term }.AsQueryable().BuildMock());

            var command = new UpdateTermCommand
            {
                TermId = termId,
                Name = "Updated Term",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
                Version = 99 // Wrong version
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
        }

        [Fact]
        public async Task Handle_OverlapDates_ReturnsConflict()
        {
            // Arrange
            var termId = Guid.NewGuid();
            var term = CreateUpcomingTerm(termId);

            var conflictingTermId = Guid.NewGuid();
            var conflictingTerm = new Term
            {
                TermId = conflictingTermId,
                UniversityId = _universityId,
                Name = "Conflicting Term",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(110)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(200)),
                Status = TermStatus.Open,
                Version = 1
            };

            // First Query() call returns the term being updated,
            // second call returns a list with the conflicting term (for overlap check)
            var callCount = 0;
            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        return new List<Term> { term }.AsQueryable().BuildMock();
                    return new List<Term> { conflictingTerm }.AsQueryable().BuildMock();
                });

            var command = new UpdateTermCommand
            {
                TermId = termId,
                Name = "Updated Term",
                // New dates overlap with conflictingTerm
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(150)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(220)),
                Version = 1
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
        }

        [Fact]
        public async Task Handle_SuperAdmin_ValidUpdate_ReturnsSuccess()
        {
            // Arrange
            var termId = Guid.NewGuid();
            var term = CreateUpcomingTerm(termId);

            var callCount = 0;
            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        return new List<Term> { term }.AsQueryable().BuildMock();
                    // No overlapping terms
                    return new List<Term>().AsQueryable().BuildMock();
                });

            _mockUnitOfWork.Setup(x => x.Repository<Term>()
                .UpdateAsync(It.IsAny<Term>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper.Setup(x => x.Map<UpdateTermResponse>(It.IsAny<Term>()))
                .Returns(new UpdateTermResponse { TermId = termId, Name = "Updated Term" });

            var command = new UpdateTermCommand
            {
                TermId = termId,
                Name = "Updated Term",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),
                Version = 1
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            _mockUnitOfWork.Verify(x => x.Repository<Term>().UpdateAsync(It.IsAny<Term>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
