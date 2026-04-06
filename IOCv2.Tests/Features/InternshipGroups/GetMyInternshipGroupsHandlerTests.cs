using FluentAssertions;
using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Queries.GetMyInternshipGroups;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace IOCv2.Tests.Features.InternshipGroups;

public class GetMyInternshipGroupsHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly Mock<ILogger<GetMyInternshipGroupsHandler>> _mockLogger;
    private readonly Mock<IGenericRepository<User>> _mockUserRepository;
    private readonly Mock<IGenericRepository<StudentTerm>> _mockStudentTermRepository;
    private readonly Mock<IGenericRepository<InternshipGroup>> _mockInternshipGroupRepository;
    private readonly Mock<IGenericRepository<Project>> _mockProjectRepository;
    private readonly Mock<IGenericRepository<EvaluationCycle>> _mockEvaluationCycleRepository;
    private readonly GetMyInternshipGroupsHandler _handler;

    public GetMyInternshipGroupsHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockMessageService = new Mock<IMessageService>();
        _mockLogger = new Mock<ILogger<GetMyInternshipGroupsHandler>>();
        _mockUserRepository = new Mock<IGenericRepository<User>>();
        _mockStudentTermRepository = new Mock<IGenericRepository<StudentTerm>>();
        _mockInternshipGroupRepository = new Mock<IGenericRepository<InternshipGroup>>();
        _mockProjectRepository = new Mock<IGenericRepository<Project>>();
        _mockEvaluationCycleRepository = new Mock<IGenericRepository<EvaluationCycle>>();

        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.Repository<User>()).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.Repository<StudentTerm>()).Returns(_mockStudentTermRepository.Object);
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.Repository<InternshipGroup>()).Returns(_mockInternshipGroupRepository.Object);
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.Repository<Project>()).Returns(_mockProjectRepository.Object);
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.Repository<EvaluationCycle>()).Returns(_mockEvaluationCycleRepository.Object);

        _handler = new GetMyInternshipGroupsHandler(
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockMessageService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserHasInternshipGroups_ShouldReturnMappedGroups()
    {
        var userId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var termId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var enterpriseId = Guid.NewGuid();
        var mentorId = Guid.NewGuid();
        var internshipId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var university = University.Create("FUCT", "FU Cần Thơ", null, null, schoolId);
        var term = new Term { TermId = termId, UniversityId = schoolId, Name = "FU Cần Thơ - Mùa xuân 2026", University = university };
        var mentorUser = new User(Guid.NewGuid(), "MENTOR001", "mentor@rikkei.vn", "Mentor Name", IOCv2.Domain.Enums.UserRole.Mentor, "hash");
        var mentor = new EnterpriseUser { EnterpriseUserId = mentorId, EnterpriseId = enterpriseId, UserId = Guid.NewGuid(), User = mentorUser };
        var group = InternshipGroup.Create(termId, "FU Cần Thơ - Mùa xuân 2026 - IOC (C#, React)", null, enterpriseId, mentorId, new DateTime(2026, 1, 13), new DateTime(2026, 4, 11));
        group.Enterprise = new Enterprise { EnterpriseId = enterpriseId, Name = "Rikasoft" };
        var phase = InternshipPhase.Create(enterpriseId, "FU Cần Thơ - Mùa xuân 2026",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(70)),
            "CNTT",
            20,
            "Test phase",
            null);
        typeof(InternshipPhase).GetProperty("PhaseId")!.SetValue(phase, termId);
        group.InternshipPhase = phase;
        group.Mentor = mentor;
        group.UpdateStatus(IOCv2.Domain.Enums.GroupStatus.Active);
        group.AddMember(studentId, IOCv2.Domain.Enums.InternshipRole.Leader);

        typeof(InternshipGroup).GetProperty(nameof(InternshipGroup.InternshipId))!.SetValue(group, internshipId);

        var project = Project.Create("IOC Version 2", string.Empty, "PRJ-IOC_IOC_1", "IT", "Requirements");
        project.AssignToGroup(internshipId, null, null);
        typeof(Project).GetProperty(nameof(Project.ProjectId))!.SetValue(project, projectId);

        var user = new User(userId, "STUDENT001", "student@example.com", "Student Name", IOCv2.Domain.Enums.UserRole.Student, "hash");
        typeof(User).GetProperty(nameof(User.Student))!.SetValue(user, new Student { StudentId = studentId, UserId = userId });

        _mockCurrentUserService.Setup(service => service.UserId).Returns(userId.ToString());
        var studentUser = new User(userId, "STU001", "student@fpt.edu.vn", "Student Name", IOCv2.Domain.Enums.UserRole.Student, "hash");
        var studentProfile = new Student { StudentId = studentId, UserId = userId };
        typeof(User).GetProperty(nameof(User.Student))!.SetValue(studentUser, studentProfile);

        _mockUserRepository.Setup(repository => repository.Query())
            .Returns(new List<User> { studentUser }.AsQueryable().BuildMock());

        _mockStudentTermRepository.Setup(repository => repository.Query())
            .Returns(new List<StudentTerm>().AsQueryable().BuildMock());
        _mockUserRepository.Setup(repository => repository.Query())
            .Returns(new List<User> { user }.AsQueryable().BuildMock());
        _mockInternshipGroupRepository.Setup(repository => repository.Query())
            .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());
        _mockProjectRepository.Setup(repository => repository.Query())
            .Returns(new List<Project> { project }.AsQueryable().BuildMock());
        _mockEvaluationCycleRepository.Setup(repository => repository.Query())
            .Returns(new List<EvaluationCycle>().AsQueryable().BuildMock());
        _mockStudentTermRepository.Setup(repository => repository.Query())
            .Returns(new List<StudentTerm>().AsQueryable().BuildMock());

        var result = await _handler.Handle(new GetMyInternshipGroupsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Id.Should().Be(internshipId);
        result.Data[0].Name.Should().Be("FU Cần Thơ - Mùa xuân 2026 - IOC (C#, React)");
        // SchoolId comes from student.User.UniversityUser.University which is not loaded via mock (returns Guid.Empty)
        result.Data[0].SchoolId.Should().Be(Guid.Empty);
        result.Data[0].Enterprise!.Name.Should().Be("Rikasoft");
        result.Data[0].Mentors.Should().ContainSingle();
        result.Data[0].ProjectId.Should().Be(projectId);
        result.Data[0].Project!.Name.Should().Be("IOC Version 2");
        result.Data[0].StudentCount.Should().Be(1);
        result.Data[0].GroupStatus.Should().Be(IOCv2.Domain.Enums.GroupStatus.Active);
        result.Data[0].HasNoMentorWarning.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsMissing_ShouldThrowUnauthorizedAccessException()
    {
        _mockCurrentUserService.Setup(service => service.UserId).Returns((string?)null);
        _mockMessageService.Setup(service => service.GetMessage(MessageKeys.Common.Unauthorized)).Returns("Unauthorized");

        var act = async () => await _handler.Handle(new GetMyInternshipGroupsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Unauthorized");
    }

    [Fact]
    public async Task Handle_WhenStudentProfileDoesNotExist_ShouldThrowNotFoundException()
    {
        _mockCurrentUserService.Setup(service => service.UserId).Returns(Guid.NewGuid().ToString());
        _mockMessageService.Setup(service => service.GetMessage(MessageKeys.Users.NotFound)).Returns("User not found");
        _mockUserRepository.Setup(repository => repository.Query())
            .Returns(new List<User>().AsQueryable().BuildMock());

        var act = async () => await _handler.Handle(new GetMyInternshipGroupsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found");
    }
}
