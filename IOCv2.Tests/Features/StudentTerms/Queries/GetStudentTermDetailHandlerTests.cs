using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.StudentTerms.Queries.GetStudentTermDetail;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace IOCv2.Tests.Features.StudentTerms.Queries;

public class GetStudentTermDetailHandlerTests
{
    [Fact]
    public async Task Handle_StudentTermNotFound_ReturnsNotFound()
    {
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockMessageService = new Mock<IMessageService>();
        var mockStudentTermRepo = new Mock<IGenericRepository<StudentTerm>>();

        mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);

        mockStudentTermRepo.Setup(x => x.Query())
            .Returns(new List<StudentTerm>().AsQueryable().BuildMock());
        mockUnitOfWork.Setup(x => x.Repository<StudentTerm>()).Returns(mockStudentTermRepo.Object);

        var handler = new GetStudentTermDetailHandler(
            mockUnitOfWork.Object,
            mockCurrentUserService.Object,
            mockMessageService.Object);

        var result = await handler.Handle(new GetStudentTermDetailQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }
}


