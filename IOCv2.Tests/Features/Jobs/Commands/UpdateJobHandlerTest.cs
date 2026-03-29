using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Jobs.Commands.UpdateJob;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using IOCv2.Application.Common.Models;
using MockQueryable;

namespace IOCv2.Tests.Features.Jobs.Commands
{
    public class UpdateJobHandlerTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ILogger<UpdateJobHandler>> _mockLogger;
        private readonly Mock<IBackgroundEmailSender> _mockEmailSender;
        private readonly Mock<IMapper> _mockMapper;
        private readonly UpdateJobHandler _handler;

        public UpdateJobHandlerTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<UpdateJobHandler>>();
            _mockEmailSender = new Mock<IBackgroundEmailSender>();
            _mockMapper = new Mock<IMapper>();

            // common UoW behaviors
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _handler = new UpdateJobHandler(
                _mockUnitOfWork.Object,
                _mockMessageService.Object,
                _mockCurrentUserService.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockEmailSender.Object);
        }

        [Fact]
        public async Task Handle_JobNotFound_ReturnsNotFound()
        {
            // Arrange
            var command = new UpdateJobCommand { JobId = Guid.NewGuid() };

            _mockUnitOfWork.Setup(x => x.Repository<Job>().Query()).Returns(new List<Job>().AsQueryable().BuildMock());
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Common.RecordNotFound)).Returns("Record not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            result.Error.Should().Be("Record not found");
        }

        [Fact]
        public async Task Handle_InvalidCurrentUserIdForEnterpriseRole_ReturnsUnauthorized()
        {
            // Arrange
            var job = Job.Create(Guid.NewGuid(), "T");
            job.JobId = Guid.NewGuid();

            _mockUnitOfWork.Setup(x => x.Repository<Job>().Query()).Returns(new List<Job> { job }.AsQueryable().BuildMock());

            _mockCurrentUserService.SetupGet(c => c.Role).Returns("HR");
            _mockCurrentUserService.SetupGet(c => c.UserId).Returns("not-a-guid");

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Common.Unauthorized)).Returns("Unauthorized");

            var command = new UpdateJobCommand { JobId = job.JobId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
            result.Error.Should().Be("Unauthorized");
        }

        [Fact]
        public async Task Handle_PublishedWithApplications_AndNoForce_ReturnsConflict()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var job = Job.Create(enterpriseId, "T");
            job.JobId = Guid.NewGuid();
            job.Status = JobStatus.PUBLISHED;
            job.InternshipApplications = new List<InternshipApplication> { new InternshipApplication(), new InternshipApplication() };

            var entUser = new EnterpriseUser { EnterpriseUserId = Guid.NewGuid(), EnterpriseId = enterpriseId, UserId = Guid.NewGuid() };

            _mockUnitOfWork.Setup(x => x.Repository<Job>().Query()).Returns(new List<Job> { job }.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().Query()).Returns(new List<EnterpriseUser> { entUser }.AsQueryable().BuildMock());

            _mockCurrentUserService.SetupGet(c => c.Role).Returns("HR");
            _mockCurrentUserService.SetupGet(c => c.UserId).Returns(entUser.UserId.ToString());

            var expectedMsg = "Confirm update with applications";
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.JobPostingMessageKey.UpdateConfirmHasApplications, It.IsAny<object[]>())).Returns(expectedMsg);

            var command = new UpdateJobCommand { JobId = job.JobId, ForceUpdateWithApplications = false };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
            result.Error.Should().Be(expectedMsg);
        }

        [Fact]
        public async Task Handle_QuantityLessThanPlaced_ReturnsBadRequest()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var job = Job.Create(enterpriseId, "T");
            job.JobId = Guid.NewGuid();
            job.InternshipApplications = new List<InternshipApplication>
            {
                new InternshipApplication { Status = InternshipApplicationStatus.Placed },
                new InternshipApplication { Status = InternshipApplicationStatus.Placed }
            };

            var entUser = new EnterpriseUser { EnterpriseUserId = Guid.NewGuid(), EnterpriseId = enterpriseId, UserId = Guid.NewGuid() };

            _mockUnitOfWork.Setup(x => x.Repository<Job>().Query()).Returns(new List<Job> { job }.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().Query()).Returns(new List<EnterpriseUser> { entUser }.AsQueryable().BuildMock());

            _mockCurrentUserService.SetupGet(c => c.Role).Returns("HR");
            _mockCurrentUserService.SetupGet(c => c.UserId).Returns(entUser.UserId.ToString());

            var expectedMsg = "Quantity less than placed";
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.JobPostingMessageKey.UpdateQuantityLessThanPlaced, It.IsAny<object[]>())).Returns(expectedMsg);

            var command = new UpdateJobCommand { JobId = job.JobId, Quantity = 1, ForceUpdateWithApplications = true };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            result.Error.Should().Be(expectedMsg);
        }

        [Fact]
        public async Task Handle_ReopenJob_NotifiesStudentsAndReturnsSuccessWithMessage()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var job = Job.Create(enterpriseId, "ReopenTitle");
            job.JobId = Guid.NewGuid();
            job.Enterprise = new Enterprise { EnterpriseId = enterpriseId, Name = "EName" };
            job.EnterpriseId = enterpriseId;
            job.Status = JobStatus.CLOSED;

            var studentUser = Domain.Entities.User.Create(Guid.NewGuid(), "student@example.com" );
            var student = new Student { StudentId = Guid.NewGuid(), UserId = studentUser.UserId, User = studentUser };

            var app = new InternshipApplication
            {
                ApplicationId = Guid.NewGuid(),
                JobId = job.JobId,
                Status = InternshipApplicationStatus.Applied,
                Student = student
            };

            job.InternshipApplications = new List<InternshipApplication> { app };

            var entUser = new EnterpriseUser { EnterpriseUserId = Guid.NewGuid(), EnterpriseId = enterpriseId, UserId = Guid.NewGuid() };

            _mockUnitOfWork.Setup(x => x.Repository<Job>().Query()).Returns(new List<Job> { job }.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().Query()).Returns(new List<EnterpriseUser> { entUser }.AsQueryable().BuildMock());

            _mockUnitOfWork.Setup(x => x.Repository<Job>().UpdateAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _mockCurrentUserService.SetupGet(c => c.Role).Returns("HR");
            _mockCurrentUserService.SetupGet(c => c.UserId).Returns(entUser.UserId.ToString());
            _mockCurrentUserService.SetupGet(c => c.UserId).Returns(entUser.UserId.ToString());

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.JobPostingMessageKey.ReopenNotifyStudentSubject, It.IsAny<object[]>())).Returns("Subject");
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.JobPostingMessageKey.ReopenNotifyStudentBody, It.IsAny<object[]>())).Returns("Body");
            var successMsg = "Reopen success";
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.JobPostingMessageKey.ReopenSuccess, It.IsAny<object[]>())).Returns(successMsg);

            // mapper returns some response object (handler returns Result with that response)
            _mockMapper.Setup(m => m.Map<UpdateJobResponse>(It.IsAny<Job>()))
                .Returns(new UpdateJobResponse { JobId = job.JobId });

            // email sender should be invoked once
            _mockEmailSender.Setup(e => e.EnqueueEmailAsync(It.Is<string>(s => s == "student@example.com"), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask)
                .Verifiable();

            var command = new UpdateJobCommand
            {
                JobId = job.JobId,
                ExpireDate = DateTime.UtcNow.AddDays(10),
                ForceUpdateWithApplications = true
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be(successMsg);
            _mockEmailSender.Verify(e => e.EnqueueEmailAsync("student@example.com", It.IsAny<string>(), It.IsAny<string>(), job.JobId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
