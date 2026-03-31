using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Projects.Commands.SwapGroup;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace IOCv2.Tests.Features.Projects;

public class SwapGroupHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IMessageService> _message = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<INotificationPushService> _push = new();
    private readonly Mock<IGenericRepository<Project>> _projectRepo = new();
    private readonly Mock<IGenericRepository<EnterpriseUser>> _enterpriseUserRepo = new();
    private readonly Mock<IGenericRepository<InternshipGroup>> _groupRepo = new();
    private readonly Mock<IGenericRepository<InternshipStudent>> _internshipStudentRepo = new();
    private readonly Mock<IGenericRepository<WorkItem>> _workItemRepo = new();
    private readonly Mock<IGenericRepository<Sprint>> _sprintRepo = new();
    private readonly Mock<IGenericRepository<Logbook>> _logbookRepo = new();
    private readonly Mock<IGenericRepository<Stakeholder>> _stakeholderRepo = new();
    private readonly Mock<IGenericRepository<Evaluation>> _evaluationRepo = new();
    private readonly Mock<IGenericRepository<ViolationReport>> _violationRepo = new();
    private readonly SwapGroupHandler _handler;

    public SwapGroupHandlerTests()
    {
        _unitOfWork.Setup(x => x.Repository<Project>()).Returns(_projectRepo.Object);
        _unitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(_enterpriseUserRepo.Object);
        _unitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(_groupRepo.Object);
        _unitOfWork.Setup(x => x.Repository<InternshipStudent>()).Returns(_internshipStudentRepo.Object);
        _unitOfWork.Setup(x => x.Repository<WorkItem>()).Returns(_workItemRepo.Object);
        _unitOfWork.Setup(x => x.Repository<Sprint>()).Returns(_sprintRepo.Object);
        _unitOfWork.Setup(x => x.Repository<Logbook>()).Returns(_logbookRepo.Object);
        _unitOfWork.Setup(x => x.Repository<Stakeholder>()).Returns(_stakeholderRepo.Object);
        _unitOfWork.Setup(x => x.Repository<Evaluation>()).Returns(_evaluationRepo.Object);
        _unitOfWork.Setup(x => x.Repository<ViolationReport>()).Returns(_violationRepo.Object);

        _unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cache.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _message.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns((string key, object[] _) => key);
        _message.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);

        _handler = new SwapGroupHandler(
            _unitOfWork.Object,
            _currentUser.Object,
            _message.Object,
            _cache.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<SwapGroupHandler>>(),
            _push.Object);
    }

    [Fact]
    public async Task Handle_SourceHasStudentsWithoutReplacement_ShouldFail()
    {
        var userId = Guid.NewGuid();
        var mentorId = Guid.NewGuid();

        var sourceGroup = InternshipGroup.Create(Guid.NewGuid(), "Source", mentorId: mentorId);
        var targetGroup = InternshipGroup.Create(Guid.NewGuid(), "Target", mentorId: mentorId);

        var project = Project.Create("P1", "D", "PRJ-1", "Software Engineering", "Req", mentorId: mentorId);
        project.AssignToGroup(sourceGroup.InternshipId, sourceGroup.StartDate, sourceGroup.EndDate);
        project.InternshipGroup = sourceGroup;

        _currentUser.Setup(x => x.UserId).Returns(userId.ToString());
        _enterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
        {
            new() { EnterpriseUserId = mentorId, UserId = userId }
        }.AsQueryable().BuildMock());

        _projectRepo.Setup(x => x.Query()).Returns(new List<Project> { project }.AsQueryable().BuildMock());
        _groupRepo.Setup(x => x.Query()).Returns(new List<InternshipGroup> { sourceGroup, targetGroup }.AsQueryable().BuildMock());
        _internshipStudentRepo.Setup(x => x.Query()).Returns(new List<InternshipStudent>
        {
            new() { InternshipId = sourceGroup.InternshipId, StudentId = Guid.NewGuid() }
        }.AsQueryable().BuildMock());

        _workItemRepo.Setup(x => x.Query()).Returns(new List<WorkItem>().AsQueryable().BuildMock());
        _sprintRepo.Setup(x => x.Query()).Returns(new List<Sprint>().AsQueryable().BuildMock());
        _logbookRepo.Setup(x => x.Query()).Returns(new List<Logbook>().AsQueryable().BuildMock());
        _stakeholderRepo.Setup(x => x.Query()).Returns(new List<Stakeholder>().AsQueryable().BuildMock());
        _evaluationRepo.Setup(x => x.Query()).Returns(new List<Evaluation>().AsQueryable().BuildMock());
        _violationRepo.Setup(x => x.Query()).Returns(new List<ViolationReport>().AsQueryable().BuildMock());

        var result = await _handler.Handle(new SwapGroupCommand
        {
            ProjectId = project.ProjectId,
            NewInternshipId = targetGroup.InternshipId
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.BadRequest);
    }

    [Fact]
    public async Task Handle_WithReplacement_ShouldMoveProjectAndAssignReplacementAtomically()
    {
        var userId = Guid.NewGuid();
        var mentorId = Guid.NewGuid();

        var sourceGroup = InternshipGroup.Create(Guid.NewGuid(), "Source", mentorId: mentorId);
        var targetGroup = InternshipGroup.Create(Guid.NewGuid(), "Target", mentorId: mentorId);

        var projectToMove = Project.Create("P1", "D", "PRJ-1", "Software Engineering", "Req", mentorId: mentorId);
        projectToMove.AssignToGroup(sourceGroup.InternshipId, sourceGroup.StartDate, sourceGroup.EndDate);
        projectToMove.InternshipGroup = sourceGroup;

        var replacementProject = Project.Create("P2", "D", "PRJ-2", "Software Engineering", "Req", mentorId: mentorId);

        _currentUser.Setup(x => x.UserId).Returns(userId.ToString());
        _enterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
        {
            new() { EnterpriseUserId = mentorId, UserId = userId }
        }.AsQueryable().BuildMock());

        _projectRepo.Setup(x => x.Query()).Returns(new List<Project> { projectToMove, replacementProject }.AsQueryable().BuildMock());
        _groupRepo.Setup(x => x.Query()).Returns(new List<InternshipGroup> { sourceGroup, targetGroup }.AsQueryable().BuildMock());
        _internshipStudentRepo.Setup(x => x.Query()).Returns(new List<InternshipStudent>
        {
            new() { InternshipId = sourceGroup.InternshipId, StudentId = Guid.NewGuid() }
        }.AsQueryable().BuildMock());

        _workItemRepo.Setup(x => x.Query()).Returns(new List<WorkItem>().AsQueryable().BuildMock());
        _sprintRepo.Setup(x => x.Query()).Returns(new List<Sprint>().AsQueryable().BuildMock());
        _logbookRepo.Setup(x => x.Query()).Returns(new List<Logbook>().AsQueryable().BuildMock());
        _stakeholderRepo.Setup(x => x.Query()).Returns(new List<Stakeholder>().AsQueryable().BuildMock());
        _evaluationRepo.Setup(x => x.Query()).Returns(new List<Evaluation>().AsQueryable().BuildMock());
        _violationRepo.Setup(x => x.Query()).Returns(new List<ViolationReport>().AsQueryable().BuildMock());

        var result = await _handler.Handle(new SwapGroupCommand
        {
            ProjectId = projectToMove.ProjectId,
            NewInternshipId = targetGroup.InternshipId,
            ReplacementProjectId = replacementProject.ProjectId
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        projectToMove.InternshipId.Should().Be(targetGroup.InternshipId);
        replacementProject.InternshipId.Should().Be(sourceGroup.InternshipId);
    }
}

