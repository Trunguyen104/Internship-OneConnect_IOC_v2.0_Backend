using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentDetail;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;

namespace IOCv2.Tests.Features.UniAdminInternship.Queries;

public class GetUniAdminStudentDetailHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly Mock<ILogger<GetUniAdminStudentDetailHandler>> _mockLogger;
    private readonly GetUniAdminStudentDetailHandler _handler;

    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _universityId = Guid.NewGuid();
    private readonly Guid _termId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();

    public GetUniAdminStudentDetailHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockMessageService = new Mock<IMessageService>();
        _mockLogger = new Mock<ILogger<GetUniAdminStudentDetailHandler>>();

        _mockMessageService
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns((string key) => key);

        _handler = new GetUniAdminStudentDetailHandler(
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

        var universityUserRepo = new Mock<IGenericRepository<UniversityUser>>();
        universityUserRepo
            .Setup(x => x.Query())
            .Returns(new List<UniversityUser> { universityUser }.AsQueryable().BuildMock());

        _mockUnitOfWork
            .Setup(x => x.Repository<UniversityUser>())
            .Returns(universityUserRepo.Object);
    }

    private Term BuildTerm(Guid? universityId = null) => new Term
    {
        TermId = _termId,
        UniversityId = universityId ?? _universityId,
        Name = "Term 2025",
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
        Status = TermStatus.Open
    };

    private void SetupTermRepo(IEnumerable<Term> terms)
    {
        var termRepo = new Mock<IGenericRepository<Term>>();
        termRepo
            .Setup(x => x.Query())
            .Returns(terms.AsQueryable().BuildMock());

        _mockUnitOfWork
            .Setup(x => x.Repository<Term>())
            .Returns(termRepo.Object);
    }

    private (StudentTerm studentTerm, Student student, User user) BuildStudentTerm()
    {
        var userId = Guid.NewGuid();
        var user = new User(userId, "SV001", "sv@test.com", "Student 1", UserRole.Student, "hash");
        var student = new Student
        {
            StudentId = _studentId,
            UserId = userId,
            User = user,
            ClassName = "CNTT1",
            Major = "Software Engineering"
        };
        var studentTerm = new StudentTerm
        {
            StudentId = _studentId,
            TermId = _termId,
            EnrollmentStatus = EnrollmentStatus.Active,
            PlacementStatus = PlacementStatus.Unplaced,
            Student = student,
            Enterprise = null,
            EnterpriseId = null
        };
        return (studentTerm, student, user);
    }

    private void SetupStudentTermRepo(IEnumerable<StudentTerm> studentTerms)
    {
        var studentTermRepo = new Mock<IGenericRepository<StudentTerm>>();
        studentTermRepo
            .Setup(x => x.Query())
            .Returns(studentTerms.AsQueryable().BuildMock());

        _mockUnitOfWork
            .Setup(x => x.Repository<StudentTerm>())
            .Returns(studentTermRepo.Object);
    }

    private void SetupEmptyInternshipStudentRepo()
    {
        var internshipStudentRepo = new Mock<IGenericRepository<InternshipStudent>>();
        internshipStudentRepo
            .Setup(x => x.Query())
            .Returns(new List<InternshipStudent>().AsQueryable().BuildMock());

        _mockUnitOfWork
            .Setup(x => x.Repository<InternshipStudent>())
            .Returns(internshipStudentRepo.Object);
    }

    private void SetupEmptyInternshipApplicationRepo()
    {
        var appRepo = new Mock<IGenericRepository<InternshipApplication>>();
        appRepo
            .Setup(x => x.Query())
            .Returns(new List<InternshipApplication>().AsQueryable().BuildMock());

        _mockUnitOfWork
            .Setup(x => x.Repository<InternshipApplication>())
            .Returns(appRepo.Object);
    }

    private void SetupViolationCountRepo(int count = 0)
    {
        var violationRepo = new Mock<IGenericRepository<ViolationReport>>();
        violationRepo
            .Setup(x => x.CountAsync(
                It.IsAny<Expression<Func<ViolationReport, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);

        _mockUnitOfWork
            .Setup(x => x.Repository<ViolationReport>())
            .Returns(violationRepo.Object);
    }

    private void SetupEvaluationCountRepo(int count = 0)
    {
        var evalRepo = new Mock<IGenericRepository<Evaluation>>();
        evalRepo
            .Setup(x => x.CountAsync(
                It.IsAny<Expression<Func<Evaluation, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);

        _mockUnitOfWork
            .Setup(x => x.Repository<Evaluation>())
            .Returns(evalRepo.Object);
    }

    // --------------- test cases ---------------

    [Fact]
    public async Task Handle_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.UserId).Returns("not-a-guid");

        var query = new GetUniAdminStudentDetailQuery
        {
            StudentId = _studentId
        };

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

        var universityUserRepo = new Mock<IGenericRepository<UniversityUser>>();
        universityUserRepo
            .Setup(x => x.Query())
            .Returns(new List<UniversityUser>().AsQueryable().BuildMock());

        _mockUnitOfWork
            .Setup(x => x.Repository<UniversityUser>())
            .Returns(universityUserRepo.Object);

        var query = new GetUniAdminStudentDetailQuery
        {
            StudentId = _studentId
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_TermNotFound_ReturnsNotFound()
    {
        // Arrange — TermId specified but no matching term in repo
        SetupValidUser();
        SetupUniversityUser();
        SetupTermRepo(new List<Term>());

        var query = new GetUniAdminStudentDetailQuery
        {
            StudentId = _studentId,
            TermId = _termId
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_NoOpenTerm_ReturnsNotFound()
    {
        // Arrange — no TermId specified, no open term found for university
        SetupValidUser();
        SetupUniversityUser();
        SetupTermRepo(new List<Term>());

        var query = new GetUniAdminStudentDetailQuery
        {
            StudentId = _studentId,
            TermId = null
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_StudentNotFound_ReturnsNotFound()
    {
        // Arrange — term found but student has no active StudentTerm in that term
        SetupValidUser();
        SetupUniversityUser();
        SetupTermRepo(new List<Term> { BuildTerm() });
        SetupStudentTermRepo(new List<StudentTerm>());

        var query = new GetUniAdminStudentDetailQuery
        {
            StudentId = _studentId,
            TermId = _termId
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        SetupValidUser();
        SetupUniversityUser();
        SetupTermRepo(new List<Term> { BuildTerm() });

        var (studentTerm, student, user) = BuildStudentTerm();
        SetupStudentTermRepo(new List<StudentTerm> { studentTerm });

        SetupEmptyInternshipStudentRepo();
        SetupEmptyInternshipApplicationRepo();
        SetupViolationCountRepo(0);
        SetupEvaluationCountRepo(0);

        var query = new GetUniAdminStudentDetailQuery
        {
            StudentId = _studentId,
            TermId = _termId
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.StudentId.Should().Be(_studentId);
        result.Data.ResolvedTermId.Should().Be(_termId);
        result.Data.TermName.Should().Be("Term 2025");
        result.Data.InternshipStatus.Should().Be(IOCv2.Application.Features.UniAdminInternship.Common.InternshipUiStatus.Unplaced);
        result.Data.ViolationCount.Should().Be(0);
        result.Data.PublishedEvaluationCount.Should().Be(0);
    }
}
