// IOCv2.Tests\Features\Jobs\Commands\CloseJobHandlerTests.cs
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.Jobs;
using IOCv2.Application.Features.Jobs.Commands.CloseJob;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IOCv2.Tests.Features.Jobs.Commands
{
    public class CloseJobHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private FakeGenericRepository _jobRepo;
        private FakeGenericRepositoryEntUser _entUserRepo;
        private readonly Mock<IBackgroundEmailSender> _emailMock;
        private readonly Mock<ICurrentUserService> _currentUserMock;
        private readonly Mock<IMessageService> _messageMock;
        private readonly Mock<ILogger<CloseJobHandler>> _loggerMock;

        public CloseJobHandlerTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _jobRepo = new FakeGenericRepository();
            _entUserRepo = new FakeGenericRepositoryEntUser();
            _emailMock = new Mock<IBackgroundEmailSender>();
            _currentUserMock = new Mock<ICurrentUserService>();
            _messageMock = new Mock<IMessageService>();
            _loggerMock = new Mock<ILogger<CloseJobHandler>>();

            _uowMock.Setup(u => u.Repository<Job>()).Returns(_jobRepo);
            _uowMock.Setup(u => u.Repository<EnterpriseUser>()).Returns(_entUserRepo);

            _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uowMock.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));

            _messageMock.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns((string key, object[] _) => key);
            _messageMock.Setup(m => m.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        }

        private CloseJobHandler CreateHandler()
            => new CloseJobHandler(
                _uowMock.Object,
                _messageMock.Object,
                _emailMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object);

        [Fact]
        public async Task Handle_ReturnsNotFound_WhenJobMissing()
        {
            _jobRepo = new FakeGenericRepository(new Job[0]);
            _uowMock.Setup(u => u.Repository<Job>()).Returns(_jobRepo);

            var handler = CreateHandler();
            var cmd = new CloseJobCommand { JobId = Guid.NewGuid() };

            var result = await handler.Handle(cmd, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
        }

        [Fact]
        public async Task Handle_ReturnsForbidden_WhenUserRoleNotEnterprise()
        {
            var job = new Job { JobId = Guid.NewGuid(), Status = JobStatus.PUBLISHED, EnterpriseId = Guid.NewGuid() };
            _jobRepo = new FakeGenericRepository(new[] { job });
            _uowMock.Setup(u => u.Repository<Job>()).Returns(_jobRepo);

            _currentUserMock.SetupGet(c => c.Role).Returns("StudentRole");
            _currentUserMock.SetupGet(c => c.UserId).Returns(Guid.NewGuid().ToString());

            var handler = CreateHandler();
            var cmd = new CloseJobCommand { JobId = job.JobId };

            var result = await handler.Handle(cmd, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.Forbidden, result.ErrorType);
        }

        [Fact]
        public async Task Handle_ReturnsUnauthorized_WhenEnterpriseRoleAndUserIdInvalid()
        {
            var job = new Job { JobId = Guid.NewGuid(), Status = JobStatus.PUBLISHED, EnterpriseId = Guid.NewGuid() };
            _jobRepo = new FakeGenericRepository(new[] { job });
            _uowMock.Setup(u => u.Repository<Job>()).Returns(_jobRepo);

            _currentUserMock.SetupGet(c => c.Role).Returns(JobsPostingParam.GetJobPostings.EnterpriseRoles.First());
            _currentUserMock.SetupGet(c => c.UserId).Returns("invalid-guid");

            var handler = CreateHandler();
            var cmd = new CloseJobCommand { JobId = job.JobId };

            var result = await handler.Handle(cmd, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.Unauthorized, result.ErrorType);
        }

        [Fact]
        public async Task Handle_ReturnsForbidden_WhenEnterpriseUserDoesNotOwnJob()
        {
            var job = new Job { JobId = Guid.NewGuid(), Status = JobStatus.PUBLISHED, EnterpriseId = Guid.NewGuid() };
            _jobRepo = new FakeGenericRepository(new[] { job });
            _uowMock.Setup(u => u.Repository<Job>()).Returns(_jobRepo);

            var userId = Guid.NewGuid();
            var entUser = new EnterpriseUser { UserId = userId, EnterpriseId = Guid.NewGuid() };
            _entUserRepo = new FakeGenericRepositoryEntUser(new[] { entUser });
            _uowMock.Setup(u => u.Repository<EnterpriseUser>()).Returns(_entUserRepo);

            _currentUserMock.SetupGet(c => c.Role).Returns(JobsPostingParam.GetJobPostings.EnterpriseRoles.First());
            _currentUserMock.SetupGet(c => c.UserId).Returns(userId.ToString());

            var handler = CreateHandler();
            var cmd = new CloseJobCommand { JobId = job.JobId };

            var result = await handler.Handle(cmd, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.Forbidden, result.ErrorType);
        }

        [Fact]
        public async Task Handle_ReturnsBadRequest_WhenJobAlreadyClosed()
        {
            var job = new Job { JobId = Guid.NewGuid(), Status = JobStatus.CLOSED, EnterpriseId = Guid.NewGuid() };
            _jobRepo = new FakeGenericRepository(new[] { job });
            _uowMock.Setup(u => u.Repository<Job>()).Returns(_jobRepo);

            var userId = Guid.NewGuid();
            var entUser = new EnterpriseUser { UserId = userId, EnterpriseId = job.EnterpriseId };
            _entUserRepo = new FakeGenericRepositoryEntUser(new[] { entUser });
            _uowMock.Setup(u => u.Repository<EnterpriseUser>()).Returns(_entUserRepo);

            _currentUserMock.SetupGet(c => c.Role).Returns(JobsPostingParam.GetJobPostings.EnterpriseRoles.First());
            _currentUserMock.SetupGet(c => c.UserId).Returns(userId.ToString());

            var handler = CreateHandler();
            var cmd = new CloseJobCommand { JobId = job.JobId };

            var result = await handler.Handle(cmd, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.BadRequest, result.ErrorType);
        }

        [Fact]
        public async Task Handle_ClosesJob_WhenNoActiveApplications()
        {
            var job = new Job
            {
                JobId = Guid.NewGuid(),
                Status = JobStatus.PUBLISHED,
                EnterpriseId = Guid.NewGuid(),
                Title = "JobTitle",
                InternshipApplications = new List<InternshipApplication>()
            };

            _jobRepo = new FakeGenericRepository(new[] { job });
            _uowMock.Setup(u => u.Repository<Job>()).Returns(_jobRepo);

            var userId = Guid.NewGuid();
            var entUser = new EnterpriseUser { UserId = userId, EnterpriseId = job.EnterpriseId };
            _entUserRepo = new FakeGenericRepositoryEntUser(new[] { entUser });
            _uowMock.Setup(u => u.Repository<EnterpriseUser>()).Returns(_entUserRepo);

            _currentUserMock.SetupGet(c => c.Role).Returns(JobsPostingParam.GetJobPostings.EnterpriseRoles.First());
            _currentUserMock.SetupGet(c => c.UserId).Returns(userId.ToString());

            var handler = CreateHandler();
            var cmd = new CloseJobCommand { JobId = job.JobId, ConfirmWhenHasActiveApplications = false };

            var result = await handler.Handle(cmd, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Contains(_jobRepo.UpdatedEntities, e => e != null && ((Job)e).Status == JobStatus.CLOSED);
            _uowMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _emailMock.Verify(e => e.EnqueueEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ReturnsConflict_WhenHasActiveApplicationsAndNotConfirmed()
        {
            var application = new InternshipApplication
            {
                Status = InternshipApplicationStatus.Applied,
                ApplicationId = Guid.NewGuid()
            };
            var job = new Job
            {
                JobId = Guid.NewGuid(),
                Status = JobStatus.PUBLISHED,
                EnterpriseId = Guid.NewGuid(),
                Title = "JobTitle",
                InternshipApplications = new List<InternshipApplication> { application }
            };

            _jobRepo = new FakeGenericRepository(new[] { job });
            _uowMock.Setup(u => u.Repository<Job>()).Returns(_jobRepo);

            var userId = Guid.NewGuid();
            var entUser = new EnterpriseUser { UserId = userId, EnterpriseId = job.EnterpriseId };
            _entUserRepo = new FakeGenericRepositoryEntUser(new[] { entUser });
            _uowMock.Setup(u => u.Repository<EnterpriseUser>()).Returns(_entUserRepo);

            _currentUserMock.SetupGet(c => c.Role).Returns(JobsPostingParam.GetJobPostings.EnterpriseRoles.First());
            _currentUserMock.SetupGet(c => c.UserId).Returns(userId.ToString());

            var handler = CreateHandler();
            var cmd = new CloseJobCommand { JobId = job.JobId, ConfirmWhenHasActiveApplications = false };

            var result = await handler.Handle(cmd, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultErrorType.Conflict, result.ErrorType);
            _emailMock.Verify(e => e.EnqueueEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ClosesJobAndNotifies_WhenHasActiveApplicationsAndConfirmed()
        {
            var studentUser = User.Create(Guid.NewGuid(), "student@example.com");
            var student = new Student { User = studentUser };
            var application = new InternshipApplication
            {
                Status = InternshipApplicationStatus.Applied,
                Student = student,
                ApplicationId = Guid.NewGuid()
            };
            var job = new Job
            {
                JobId = Guid.NewGuid(),
                Status = JobStatus.PUBLISHED,
                EnterpriseId = Guid.NewGuid(),
                Title = "JobTitle",
                InternshipApplications = new List<InternshipApplication> { application }
            };

            _jobRepo = new FakeGenericRepository(new[] { job });
            _uowMock.Setup(u => u.Repository<Job>()).Returns(_jobRepo);

            var userId = Guid.NewGuid();
            var entUser = new EnterpriseUser { UserId = userId, EnterpriseId = job.EnterpriseId };
            _entUserRepo = new FakeGenericRepositoryEntUser(new[] { entUser });
            _uowMock.Setup(u => u.Repository<EnterpriseUser>()).Returns(_entUserRepo);

            _currentUserMock.SetupGet(c => c.Role).Returns(JobsPostingParam.GetJobPostings.EnterpriseRoles.First());
            _currentUserMock.SetupGet(c => c.UserId).Returns(userId.ToString());

            var handler = CreateHandler();
            var cmd = new CloseJobCommand { JobId = job.JobId, ConfirmWhenHasActiveApplications = true };

            var result = await handler.Handle(cmd, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Contains(_jobRepo.UpdatedEntities, e => e != null && ((Job)e).Status == JobStatus.CLOSED);
            _uowMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _emailMock.Verify(e => e.EnqueueEmailAsync(
                It.Is<string>(s => s == "student@example.com"),
                It.IsAny<string>(),
                It.IsAny<string>(),
                job.JobId,
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        // Private helper types
        private class FakeGenericRepository : IOCv2.Application.Interfaces.IGenericRepository<Job>
        {
            private readonly List<Job> _store;
            public List<object?> UpdatedEntities { get; } = new();

            public FakeGenericRepository(IEnumerable<Job>? seed = null) => _store = seed?.ToList() ?? new List<Job>();

            public Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult<Job?>(_store.FirstOrDefault(j => j.JobId == id));

            public Task<IEnumerable<Job>> GetAllAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IEnumerable<Job>>(_store.ToList());

            public IQueryable<Job> Query() => new TestAsyncEnumerable<Job>(_store);

            public Task<IEnumerable<Job>> FindAsync(Expression<Func<Job, bool>> predicate, CancellationToken cancellationToken = default)
                => Task.FromResult<IEnumerable<Job>>(_store.AsQueryable().Where(predicate).AsEnumerable());

            public Task<Job> AddAsync(Job entity, CancellationToken cancellationToken = default)
            {
                _store.Add(entity);
                return Task.FromResult(entity);
            }

            public Task<IEnumerable<Job>> AddRangeAsync(IEnumerable<Job> entities, CancellationToken cancellationToken = default)
            {
                var added = entities.ToList();
                _store.AddRange(added);
                return Task.FromResult<IEnumerable<Job>>(added);
            }

            public Task UpdateAsync(Job entity, CancellationToken cancellationToken = default)
            {
                UpdatedEntities.Add(entity);
                return Task.CompletedTask;
            }

            public Task DeleteAsync(Job entity, CancellationToken cancellationToken = default)
            {
                _store.Remove(entity);
                return Task.CompletedTask;
            }

            public Task HardDeleteAsync(Job entity, CancellationToken cancellationToken = default)
            {
                _store.Remove(entity);
                return Task.CompletedTask;
            }

            public Task<bool> ExistsAsync(Expression<Func<Job, bool>> predicate, CancellationToken cancellationToken = default)
                => Task.FromResult(_store.AsQueryable().Any(predicate));

            public Task<int> CountAsync(Expression<Func<Job, bool>>? predicate = null, CancellationToken cancellationToken = default)
                => Task.FromResult(predicate is null ? _store.Count : _store.AsQueryable().Count(predicate));
        }

        private class FakeGenericRepositoryEntUser : IOCv2.Application.Interfaces.IGenericRepository<EnterpriseUser>
        {
            private readonly List<EnterpriseUser> _store;

            public FakeGenericRepositoryEntUser(IEnumerable<EnterpriseUser>? seed = null)
                => _store = seed?.ToList() ?? new List<EnterpriseUser>();

            public Task<EnterpriseUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult<EnterpriseUser?>(_store.FirstOrDefault(e => e.UserId == id));

            public Task<IEnumerable<EnterpriseUser>> GetAllAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IEnumerable<EnterpriseUser>>(_store.ToList());

            public IQueryable<EnterpriseUser> Query() => new TestAsyncEnumerable<EnterpriseUser>(_store);

            public Task<IEnumerable<EnterpriseUser>> FindAsync(Expression<Func<EnterpriseUser, bool>> predicate, CancellationToken cancellationToken = default)
                => Task.FromResult<IEnumerable<EnterpriseUser>>(_store.AsQueryable().Where(predicate).AsEnumerable());

            public Task<EnterpriseUser> AddAsync(EnterpriseUser entity, CancellationToken cancellationToken = default)
            {
                _store.Add(entity);
                return Task.FromResult(entity);
            }

            public Task<IEnumerable<EnterpriseUser>> AddRangeAsync(IEnumerable<EnterpriseUser> entities, CancellationToken cancellationToken = default)
            {
                var added = entities.ToList();
                _store.AddRange(added);
                return Task.FromResult<IEnumerable<EnterpriseUser>>(added);
            }

            public Task UpdateAsync(EnterpriseUser entity, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task DeleteAsync(EnterpriseUser entity, CancellationToken cancellationToken = default)
            {
                _store.Remove(entity);
                return Task.CompletedTask;
            }

            public Task HardDeleteAsync(EnterpriseUser entity, CancellationToken cancellationToken = default)
            {
                _store.Remove(entity);
                return Task.CompletedTask;
            }

            public Task<bool> ExistsAsync(Expression<Func<EnterpriseUser, bool>> predicate, CancellationToken cancellationToken = default)
                => Task.FromResult(_store.AsQueryable().Any(predicate));

            public Task<int> CountAsync(Expression<Func<EnterpriseUser, bool>>? predicate = null, CancellationToken cancellationToken = default)
                => Task.FromResult(predicate is null ? _store.Count : _store.AsQueryable().Count(predicate));
        }

        private class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
            public TestAsyncEnumerable(Expression expression) : base(expression) { }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => new TestAsyncEnumerator<T>(((IEnumerable<T>)this).GetEnumerator());

            IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
        }

        private class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;

            public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

            public T Current => _inner.Current;

            public ValueTask DisposeAsync()
            {
                _inner.Dispose();
                return ValueTask.CompletedTask;
            }

            public ValueTask<bool> MoveNextAsync()
                => new ValueTask<bool>(_inner.MoveNext());
        }

        private class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
        {
            private readonly IQueryProvider _inner;

            internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

            public IQueryable CreateQuery(Expression expression)
            {
                var elementType = expression.Type.GetGenericArguments().FirstOrDefault() ?? typeof(TEntity);
                var queryableType = typeof(EnumerableQuery<>).MakeGenericType(elementType);
                return (IQueryable)Activator.CreateInstance(queryableType, expression)!;
            }

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                return new EnumerableQuery<TElement>(expression);
            }

            public object? Execute(Expression expression)
            {
                return _inner.Execute(expression);
            }

            public TResult Execute<TResult>(Expression expression)
            {
                var isAsync = typeof(TResult).IsGenericType &&
                              typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>);

                if (isAsync)
                {
                    var resultType = typeof(TResult).GetGenericArguments()[0];
                    var executeMethod = typeof(IQueryProvider)
                        .GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(Expression) })
                        ?.MakeGenericMethod(resultType);

                    var result = executeMethod?.Invoke(_inner, new[] { expression });
                    var taskMethod = typeof(Task)
                        .GetMethod(nameof(Task.FromResult))
                        ?.MakeGenericMethod(resultType);

                    return (TResult)(taskMethod?.Invoke(null, new[] { result }) ?? Task.FromResult(result));
                }

                return _inner.Execute<TResult>(expression);
            }

            public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
            {
                return Execute<TResult>(expression);
            }
        }
    }
}