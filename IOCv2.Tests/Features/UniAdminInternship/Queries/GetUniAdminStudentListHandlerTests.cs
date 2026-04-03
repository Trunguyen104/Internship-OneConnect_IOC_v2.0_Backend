using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentList;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace IOCv2.Tests.Features.UniAdminInternship.Queries;

public class GetUniAdminStudentListHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly Mock<ILogger<GetUniAdminStudentListHandler>> _mockLogger;
    private readonly GetUniAdminStudentListHandler _handler;

    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _universityId = Guid.NewGuid();
    private readonly Guid _termId = Guid.NewGuid();

    public GetUniAdminStudentListHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockMessageService = new Mock<IMessageService>();
        _mockLogger = new Mock<ILogger<GetUniAdminStudentListHandler>>();

        _mockMessageService
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns((string key) => key);

        _handler = new GetUniAdminStudentListHandler(
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockMessageService.Object,
            _mockLogger.Object);
    }

    // --------------- helpers ---------------

    private void SetupValidUser()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_currentUserId.ToString());
    }

    private void SetupUniversityUser()
    {
        var universityUser = new UniversityUser
        {
            UserId = _currentUserId,
            UniversityId = _universityId
        };

        var repo = new Mock<IGenericRepository<UniversityUser>>();
        repo.Setup(x => x.Query())
            .Returns(new List<UniversityUser> { universityUser }.AsQueryable().BuildMock());

        _mockUnitOfWork.Setup(x => x.Repository<UniversityUser>()).Returns(repo.Object);
    }

    private Term BuildOpenTerm() => new Term
    {
        TermId = _termId,
        UniversityId = _universityId,
        Name = "Term 2025",
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
        Status = TermStatus.Open
    };

    private void SetupTermRepo(IEnumerable<Term> terms)
    {
        var repo = new Mock<IGenericRepository<Term>>();
        repo.Setup(x => x.Query()).Returns(terms.AsQueryable().BuildMock());
        _mockUnitOfWork.Setup(x => x.Repository<Term>()).Returns(repo.Object);
    }

    private void SetupStudentTermRepo(IEnumerable<StudentTerm> studentTerms)
    {
        var repo = new Mock<IGenericRepository<StudentTerm>>();
        repo.Setup(x => x.Query()).Returns(studentTerms.AsQueryable().BuildMock());
        _mockUnitOfWork.Setup(x => x.Repository<StudentTerm>()).Returns(repo.Object);
    }

    private void SetupEmptyAuxiliaryRepos()
    {
        var internshipStudentRepo = new Mock<IGenericRepository<InternshipStudent>>();
        internshipStudentRepo.Setup(x => x.Query())
            .Returns(new List<InternshipStudent>().AsQueryable().BuildMock());
        _mockUnitOfWork.Setup(x => x.Repository<InternshipStudent>()).Returns(internshipStudentRepo.Object);

        var logbookRepo = new Mock<IGenericRepository<Logbook>>();
        logbookRepo.Setup(x => x.Query())
            .Returns(new List<Logbook>().AsQueryable().BuildMock());
        _mockUnitOfWork.Setup(x => x.Repository<Logbook>()).Returns(logbookRepo.Object);

        var violationRepo = new Mock<IGenericRepository<ViolationReport>>();
        violationRepo.Setup(x => x.Query())
            .Returns(new List<ViolationReport>().AsQueryable().BuildMock());
        _mockUnitOfWork.Setup(x => x.Repository<ViolationReport>()).Returns(violationRepo.Object);

        var appRepo = new Mock<IGenericRepository<InternshipApplication>>();
        appRepo.Setup(x => x.Query())
            .Returns(new List<InternshipApplication>().AsQueryable().BuildMock());
        _mockUnitOfWork.Setup(x => x.Repository<InternshipApplication>()).Returns(appRepo.Object);
    }

    private StudentTerm BuildStudentTerm(Guid studentId, string fullName, string userCode)
    {
        var userId = Guid.NewGuid();
        var user = new User(userId, userCode, $"{userCode}@test.com", fullName, UserRole.Student, "hash");
        var student = new Student
        {
            StudentId = studentId,
            UserId = userId,
            User = user,
            ClassName = "CNTT1",
            Major = "Software Engineering"
        };
        return new StudentTerm
        {
            StudentId = studentId,
            TermId = _termId,
            EnrollmentStatus = EnrollmentStatus.Active,
            PlacementStatus = PlacementStatus.Unplaced,
            Student = student,
            Enterprise = null,
            EnterpriseId = null
        };
    }

    // --------------- test cases ---------------

    [Fact]
    public async Task Handle_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.UserId).Returns("not-a-guid");

        var query = new GetUniAdminStudentListQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_UniversityUserNotFound_ReturnsForbidden()
    {
        // Arrange
        SetupValidUser();

        var repo = new Mock<IGenericRepository<UniversityUser>>();
        repo.Setup(x => x.Query())
            .Returns(new List<UniversityUser>().AsQueryable().BuildMock());
        _mockUnitOfWork.Setup(x => x.Repository<UniversityUser>()).Returns(repo.Object);

        var query = new GetUniAdminStudentListQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_NoOpenTerm_WithNoTermId_ReturnsEmptySuccess()
    {
        // Arrange — no TermId specified, no open term found → return empty success (not error)
        SetupValidUser();
        SetupUniversityUser();
        SetupTermRepo(new List<Term>());

        var query = new GetUniAdminStudentListQuery
        {
            TermId = null,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Students.Items.Should().BeEmpty();
        result.Data.Summary.TotalStudents.Should().Be(0);
        result.Data.ResolvedTermId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Handle_TermNotFound_WithTermId_ReturnsNotFound()
    {
        // Arrange — TermId specified but term does not exist
        SetupValidUser();
        SetupUniversityUser();
        SetupTermRepo(new List<Term>());

        var query = new GetUniAdminStudentListQuery
        {
            TermId = _termId,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_NoStudents_ReturnsEmptySuccess()
    {
        // Arrange — term exists but has no active student enrollments
        SetupValidUser();
        SetupUniversityUser();
        SetupTermRepo(new List<Term> { BuildOpenTerm() });
        SetupStudentTermRepo(new List<StudentTerm>());
        SetupEmptyAuxiliaryRepos();

        var query = new GetUniAdminStudentListQuery
        {
            TermId = _termId,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Students.Items.Should().BeEmpty();
        result.Data.Summary.TotalStudents.Should().Be(0);
        result.Data.ResolvedTermId.Should().Be(_termId);
    }

    [Fact]
    public async Task Handle_ValidRequest_WithStudents_ReturnsSuccess()
    {
        // Arrange
        SetupValidUser();
        SetupUniversityUser();
        SetupTermRepo(new List<Term> { BuildOpenTerm() });

        var studentId1 = Guid.NewGuid();
        var studentId2 = Guid.NewGuid();
        var st1 = BuildStudentTerm(studentId1, "Alice Nguyen", "SV001");
        var st2 = BuildStudentTerm(studentId2, "Bob Tran", "SV002");

        SetupStudentTermRepo(new List<StudentTerm> { st1, st2 });
        SetupEmptyAuxiliaryRepos();

        var query = new GetUniAdminStudentListQuery
        {
            TermId = _termId,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Students.Items.Should().HaveCount(2);
        result.Data.Summary.TotalStudents.Should().Be(2);
        result.Data.Summary.Unplaced.Should().Be(2);
        result.Data.Summary.Placed.Should().Be(0);
        result.Data.ResolvedTermId.Should().Be(_termId);

        var studentCodes = result.Data.Students.Items.Select(s => s.StudentCode).ToList();
        studentCodes.Should().Contain("SV001");
        studentCodes.Should().Contain("SV002");
    }
}
