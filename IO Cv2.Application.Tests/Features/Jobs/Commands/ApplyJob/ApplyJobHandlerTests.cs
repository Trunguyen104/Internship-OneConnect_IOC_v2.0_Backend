using IOCv2.Application.Constants;
using IOCv2.Application.Features.Jobs.Commands.ApplyJob;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Common.Models;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IOCv2.Application.Tests.Features.Jobs.Commands.ApplyJob
{
    public class ApplyJobHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow = new();
        private readonly Mock<IMessageService> _mockMessageService = new();
        private readonly Mock<ICurrentUserService> _mockCurrentUser = new();
        private readonly Mock<ILogger<ApplyJobHandler>> _mockLogger = new();
        private readonly Mock<IPublisher> _mockPublisher = new();

        public ApplyJobHandlerTests()
        {
            _mockMessageService.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string key, object[] args) => key);
        }

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUser.SetupGet(c => c.UserId).Returns("not-a-guid");

            var handler = new ApplyJobHandler(_mockUow.Object, _mockMessageService.Object, _mockCurrentUser.Object, _mockLogger.Object, _mockPublisher.Object);

            var request = new ApplyJobCommand { JobId = Guid.NewGuid() };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.Unauthorized, result.ErrorType);
        }

        [Fact]
        public async Task Handle_StudentMissingCv_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockCurrentUser.SetupGet(c => c.UserId).Returns(userId.ToString());

            var student = new Student
            {
                StudentId = Guid.NewGuid(),
                UserId = userId,
                CvUrl = null, // missing CV
                InternshipStatus = StudentStatus.Unplaced,
                User = new User { UserId = userId, FullName = "Student A" }
            };

            var studentRepo = new Mock<IRepository<Student>>();
            studentRepo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Student>(new[] { student }).AsQueryable());
            _mockUow.Setup(u => u.Repository<Student>()).Returns(studentRepo.Object);

            var handler = new ApplyJobHandler(_mockUow.Object, _mockMessageService.Object, _mockCurrentUser.Object, _mockLogger.Object, _mockPublisher.Object);
            var request = new ApplyJobCommand { JobId = Guid.NewGuid() };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
        }

        [Fact]
        public async Task Handle_Success_CreatesApplicationAndPublishes()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockCurrentUser.SetupGet(c => c.UserId).Returns(userId.ToString());

            var studentId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var termId = Guid.NewGuid();

            var student = new Student
            {
                StudentId = studentId,
                UserId = userId,
                CvUrl = "https://cv.example/resume.pdf",
                InternshipStatus = StudentStatus.Unplaced,
                User = new User { UserId = userId, FullName = "Student A" }
            };

            var job = new Job
            {
                JobId = jobId,
                EnterpriseId = enterpriseId,
                Title = "Intern Developer",
                Status = JobStatus.PUBLISHED,
                ExpireDate = null,
                Enterprise = new Enterprise { EnterpriseId = enterpriseId, Name = "Acme Corp" },
                InternshipApplications = new List<InternshipApplication>()
            };

            var term = new Domain.Entities.Term
            {
                TermId = termId,
                Status = TermStatus.Open,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
            };

            // No active applications
            var internshipApps = Array.Empty<InternshipApplication>();

            // HR users
            var hrUser = new User { UserId = Guid.NewGuid(), Role = UserRole.HR, FullName = "HR Person" };
            var enterpriseUser = new EnterpriseUser { EnterpriseId = enterpriseId, User = hrUser };

            // Student repo
            var studentRepo = new Mock<IRepository<Student>>();
            studentRepo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Student>(new[] { student }).AsQueryable());
            _mockUow.Setup(u => u.Repository<Student>()).Returns(studentRepo.Object);

            // Job repo
            var jobRepo = new Mock<IRepository<Job>>();
            jobRepo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Job>(new[] { job }).AsQueryable());
            _mockUow.Setup(u => u.Repository<Job>()).Returns(jobRepo.Object);

            // Term repo
            var termRepo = new Mock<IRepository<Domain.Entities.Term>>();
            termRepo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<Domain.Entities.Term>(new[] { term }).AsQueryable());
            _mockUow.Setup(u => u.Repository<Domain.Entities.Term>()).Returns(termRepo.Object);

            // InternshipApplication repo - for AnyAsync and CountAsync and AddAsync
            var appRepo = new Mock<IRepository<InternshipApplication>>();
            appRepo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<InternshipApplication>(internshipApps).AsQueryable());
            appRepo.Setup(r => r.AddAsync(It.IsAny<InternshipApplication>())).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.Repository<InternshipApplication>()).Returns(appRepo.Object);

            // EnterpriseUser repo
            var euRepo = new Mock<IRepository<EnterpriseUser>>();
            euRepo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<EnterpriseUser>(new[] { enterpriseUser }).AsQueryable());
            _mockUow.Setup(u => u.Repository<EnterpriseUser>()).Returns(euRepo.Object);

            // Transaction and save
            _mockUow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));
            _mockUow.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _mockPublisher.Setup(p => p.Publish(It.IsAny<ApplicationSubmittedEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var handler = new ApplyJobHandler(_mockUow.Object, _mockMessageService.Object, _mockCurrentUser.Object, _mockLogger.Object, _mockPublisher.Object);

            var request = new ApplyJobCommand { JobId = jobId };

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.IsType<ApplyJobResponse>(result.Value);
            _mockPublisher.Verify();
            appRepo.Verify(r => r.AddAsync(It.IsAny<InternshipApplication>()), Times.Once);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    // EF Core async query helpers used for unit-testing IQueryable async extensions (FirstOrDefaultAsync, AnyAsync, CountAsync, ToListAsync)
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments().First();
            var queryableType = typeof(TestAsyncEnumerable<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(queryableType, expression)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object? Execute(Expression expression) => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        {
            return new TestAsyncEnumerable<TResult>(expression);
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var result = Execute<TResult>(expression);
            return Task.FromResult(result);
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }

        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return default;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }
    }
}