using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Queries.GetAvailableMentors;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;

namespace IOCv2.Tests.Features.InternshipGroups.Queries;

public class GetAvailableMentorsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsForbidden_WhenCurrentUserIsNotHr()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUserService>();
        var messageService = new Mock<IMessageService>();

        currentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        currentUser.Setup(x => x.Role).Returns(UserRole.EnterpriseAdmin.ToString());
        messageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);

        var handler = new GetAvailableMentorsHandler(
            unitOfWork.Object,
            currentUser.Object,
            messageService.Object,
            Mock.Of<ILogger<GetAvailableMentorsHandler>>());

        var result = await handler.Handle(new GetAvailableMentorsQuery { InternshipGroupId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyActiveMentors_AndSupportsSearch()
    {
        var currentUserId = Guid.NewGuid();
        var enterpriseId = Guid.NewGuid();

        var currentEnterpriseUser = new EnterpriseUser
        {
            UserId = currentUserId,
            EnterpriseId = enterpriseId,
            EnterpriseUserId = Guid.NewGuid()
        };

        var matchingMentorUser = new User(Guid.NewGuid(), "M001", "mentor.active@company.com", "Mentor Active", UserRole.Mentor, "hash");
        matchingMentorUser.SetStatus(UserStatus.Active);
        var matchingMentor = new EnterpriseUser
        {
            UserId = matchingMentorUser.UserId,
            EnterpriseId = enterpriseId,
            EnterpriseUserId = Guid.NewGuid(),
            User = matchingMentorUser
        };

        var inactiveMentorUser = new User(Guid.NewGuid(), "M002", "mentor.inactive@company.com", "Mentor Inactive", UserRole.Mentor, "hash");
        inactiveMentorUser.SetStatus(UserStatus.Inactive);
        var inactiveMentor = new EnterpriseUser
        {
            UserId = inactiveMentorUser.UserId,
            EnterpriseId = enterpriseId,
            EnterpriseUserId = Guid.NewGuid(),
            User = inactiveMentorUser
        };

        var anotherActiveMentorUser = new User(Guid.NewGuid(), "M003", "another@company.com", "Another Mentor", UserRole.Mentor, "hash");
        anotherActiveMentorUser.SetStatus(UserStatus.Active);
        var anotherActiveMentor = new EnterpriseUser
        {
            UserId = anotherActiveMentorUser.UserId,
            EnterpriseId = enterpriseId,
            EnterpriseUserId = Guid.NewGuid(),
            User = anotherActiveMentorUser
        };

        var group = InternshipGroup.Create(
            phaseId: Guid.NewGuid(),
            groupName: "Group A",
            enterpriseId: enterpriseId);

        var enterpriseRepo = new Mock<IGenericRepository<EnterpriseUser>>();
        var enterpriseQueryCall = 0;
        enterpriseRepo.Setup(x => x.Query()).Returns(() =>
        {
            enterpriseQueryCall++;
            if (enterpriseQueryCall == 1)
            {
                return new List<EnterpriseUser> { currentEnterpriseUser }.AsQueryable().BuildMock();
            }

            return new List<EnterpriseUser> { matchingMentor, inactiveMentor, anotherActiveMentor }.AsQueryable().BuildMock();
        });

        var groupRepo = new Mock<IGenericRepository<InternshipGroup>>();
        groupRepo.Setup(x => x.Query()).Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(enterpriseRepo.Object);
        unitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(groupRepo.Object);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(currentUserId.ToString());
        currentUser.Setup(x => x.Role).Returns(UserRole.HR.ToString());

        var messageService = new Mock<IMessageService>();
        messageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        messageService.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns((string key, object[] _) => key);

        var handler = new GetAvailableMentorsHandler(
            unitOfWork.Object,
            currentUser.Object,
            messageService.Object,
            Mock.Of<ILogger<GetAvailableMentorsHandler>>());

        var result = await handler.Handle(new GetAvailableMentorsQuery
        {
            InternshipGroupId = group.InternshipId,
            SearchTerm = "active@company"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle();
        result.Data![0].Email.Should().Be("mentor.active@company.com");
    }

    [Fact]
    public async Task Handle_AllowsFinishedGroup_WhenEndDateNotPassed()
    {
        var currentUserId = Guid.NewGuid();
        var enterpriseId = Guid.NewGuid();

        var currentEnterpriseUser = new EnterpriseUser
        {
            UserId = currentUserId,
            EnterpriseId = enterpriseId,
            EnterpriseUserId = Guid.NewGuid()
        };

        var mentorUser = new User(Guid.NewGuid(), "M001", "mentor@company.com", "Mentor Active", UserRole.Mentor, "hash");
        mentorUser.SetStatus(UserStatus.Active);
        var mentor = new EnterpriseUser
        {
            UserId = mentorUser.UserId,
            EnterpriseId = enterpriseId,
            EnterpriseUserId = Guid.NewGuid(),
            User = mentorUser
        };

        var group = InternshipGroup.Create(
            phaseId: Guid.NewGuid(),
            groupName: "Group A",
            enterpriseId: enterpriseId,
            startDate: DateTime.UtcNow.AddDays(-1),
            endDate: DateTime.UtcNow.AddDays(7));
        group.UpdateStatus(GroupStatus.Finished);

        var enterpriseRepo = new Mock<IGenericRepository<EnterpriseUser>>();
        var enterpriseQueryCall = 0;
        enterpriseRepo.Setup(x => x.Query()).Returns(() =>
        {
            enterpriseQueryCall++;
            if (enterpriseQueryCall == 1)
            {
                return new List<EnterpriseUser> { currentEnterpriseUser }.AsQueryable().BuildMock();
            }

            return new List<EnterpriseUser> { mentor }.AsQueryable().BuildMock();
        });

        var groupRepo = new Mock<IGenericRepository<InternshipGroup>>();
        groupRepo.Setup(x => x.Query()).Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(enterpriseRepo.Object);
        unitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(groupRepo.Object);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(currentUserId.ToString());
        currentUser.Setup(x => x.Role).Returns(UserRole.HR.ToString());

        var messageService = new Mock<IMessageService>();
        messageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        messageService.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns((string key, object[] _) => key);

        var handler = new GetAvailableMentorsHandler(
            unitOfWork.Object,
            currentUser.Object,
            messageService.Object,
            Mock.Of<ILogger<GetAvailableMentorsHandler>>());

        var result = await handler.Handle(new GetAvailableMentorsQuery
        {
            InternshipGroupId = group.InternshipId
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle();
    }
}
