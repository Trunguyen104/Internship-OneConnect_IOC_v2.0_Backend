using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Terms.Commands.CreateTerm;
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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IOCv2.Tests.Features.Terms.Commands
{
    public class CreateTermHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<CreateTermHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly CreateTermHandler _handler;

        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _universityId = Guid.NewGuid();

        public CreateTermHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<CreateTermHandler>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCacheService = new Mock<ICacheService>();

            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

            _handler = new CreateTermHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockMessageService.Object,
                _mockLogger.Object,
                _mockCurrentUserService.Object,
                _mockCacheService.Object);
        }

        [Fact]
        public async Task Handle_SuperAdmin_UniversityIdMissing_ReturnsBadRequest()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");

            var command = new CreateTermCommand
            {
                Name = "Term 2025",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
                UniversityId = null
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_SuperAdmin_UniversityNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");

            _mockUnitOfWork.Setup(x => x.Repository<University>()
                .ExistsAsync(It.IsAny<Expression<Func<University, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var command = new CreateTermCommand
            {
                Name = "Term 2025",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
                UniversityId = _universityId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_SchoolAdmin_UserNotAssociated_ReturnsNotFound()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.Role).Returns("SchoolAdmin");

            _mockUnitOfWork.Setup(x => x.Repository<UniversityUser>().Query())
                .Returns(new List<UniversityUser>().AsQueryable().BuildMock());

            var command = new CreateTermCommand
            {
                Name = "Term 2025",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100))
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_DuplicateName_ReturnsConflict()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");

            _mockUnitOfWork.Setup(x => x.Repository<University>()
                .ExistsAsync(It.IsAny<Expression<Func<University, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var existingTerm = new Term
            {
                TermId = Guid.NewGuid(),
                UniversityId = _universityId,
                Name = "Term 2025",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
                Status = TermStatus.Open,
                Version = 1
            };

            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term> { existingTerm }.AsQueryable().BuildMock());

            var command = new CreateTermCommand
            {
                Name = "Term 2025",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(200)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(300)),
                UniversityId = _universityId
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
            _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");

            _mockUnitOfWork.Setup(x => x.Repository<University>()
                .ExistsAsync(It.IsAny<Expression<Func<University, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
            var end = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100));

            var existingTerm = new Term
            {
                TermId = Guid.NewGuid(),
                UniversityId = _universityId,
                Name = "Existing Term",
                StartDate = start,
                EndDate = end,
                Status = TermStatus.Open,
                Version = 1
            };

            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term> { existingTerm }.AsQueryable().BuildMock());

            // New term with different name but overlapping dates
            var command = new CreateTermCommand
            {
                Name = "New Overlapping Term",
                StartDate = start.AddDays(5),
                EndDate = end.AddDays(10),
                UniversityId = _universityId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
        }

        [Fact]
        public async Task Handle_SuperAdmin_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");

            _mockUnitOfWork.Setup(x => x.Repository<University>()
                .ExistsAsync(It.IsAny<Expression<Func<University, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Empty list → no duplicate name, no overlap
            _mockUnitOfWork.Setup(x => x.Repository<Term>().Query())
                .Returns(new List<Term>().AsQueryable().BuildMock());

            _mockUnitOfWork.Setup(x => x.Repository<Term>()
                .AddAsync(It.IsAny<Term>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Term t, CancellationToken _) => t);

            _mockMapper.Setup(x => x.Map<CreateTermResponse>(It.IsAny<Term>()))
                .Returns(new CreateTermResponse { Name = "Term 2025" });

            var command = new CreateTermCommand
            {
                Name = "Term 2025",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
                UniversityId = _universityId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            _mockUnitOfWork.Verify(x => x.Repository<Term>().AddAsync(It.IsAny<Term>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
