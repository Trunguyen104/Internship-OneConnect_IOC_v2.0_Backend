using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IOCv2.Tests.Features.InternshipGroups
{
    public class CreateInternshipGroupHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<CreateInternshipGroupHandler>> _mockLogger;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly CreateInternshipGroupHandler _handler;

        public CreateInternshipGroupHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CreateInternshipGroupHandler>>();
            _mockCacheService = new Mock<ICacheService>();

            // Inject dependencies
            _handler = new CreateInternshipGroupHandler(
                _mockUnitOfWork.Object,
                _mockMessageService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockCacheService.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var termId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var mentorId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            var command = new CreateInternshipGroupCommand
            {
                TermId = termId,
                GroupName = "Test Group",
                EnterpriseId = enterpriseId,
                MentorId = mentorId,
                Students = new List<CreateInternshipStudentDto>
                {
                    new CreateInternshipStudentDto { StudentId = studentId, Role = IOCv2.Domain.Enums.InternshipRole.Leader }

                }
            };

            // Mock behavior
            // Handler return true -> existed
            // Expression<Func<T>> = ExistsAsync(x => x.Id == termId)
            // It.IsAny<Expression<Func<Term, bool>>>() = accept any expression
            _mockUnitOfWork.Setup(x => x.Repository<Term>().ExistsAsync(It.IsAny<Expression<Func<Term, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Repository<Enterprise>().ExistsAsync(It.IsAny<Expression<Func<Enterprise, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().ExistsAsync(It.IsAny<Expression<Func<EnterpriseUser, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            //Mock FindAsync return list student
            _mockUnitOfWork.Setup(x => x.Repository<Student>().FindAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Student> { new Student { StudentId = studentId, UserId = Guid.NewGuid() } });

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().AddAsync(It.IsAny<InternshipGroup>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((InternshipGroup g, CancellationToken c) => g);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper.Setup(x => x.Map<CreateInternshipGroupResponse>(It.IsAny<InternshipGroup>()))
                .Returns(new CreateInternshipGroupResponse { GroupName = "Test Group" });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue(); //expect result true
            //Check if transaction is started and committed
            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_TermNotFound_ShouldReturnFailure()
        {
            // Arrange
            var termId = Guid.NewGuid();
            var command = new CreateInternshipGroupCommand { TermId = termId, GroupName = "Test" };

            _mockUnitOfWork.Setup(x => x.Repository<Term>().ExistsAsync(It.IsAny<Expression<Func<Term, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockMessageService.Setup(x => x.GetMessage(MessageKeys.InternshipGroups.TermNotFound))
                .Returns("Term not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            result.Error.Should().Be("Term not found");
        }
    }
}
