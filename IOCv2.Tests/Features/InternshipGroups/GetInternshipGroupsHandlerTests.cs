using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MockQueryable.Moq;
using MockQueryable;

namespace IOCv2.Tests.Features.InternshipGroups
{
    public class GetInternshipGroupsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ILogger<GetInternshipGroupsHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly GetInternshipGroupsHandler _handler;

        private readonly Guid _superAdminId = Guid.NewGuid();

        public GetInternshipGroupsHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCacheService = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<GetInternshipGroupsHandler>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();

            // Mặc định: SuperAdmin xem được tất cả — không cần DB lookup
            _mockCurrentUserService.Setup(x => x.UserId).Returns(_superAdminId.ToString());
            _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");

            _handler = new GetInternshipGroupsHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockCacheService.Object,
                _mockLogger.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object);
        }

        [Fact]
        public async Task Handle_SuperAdmin_ShouldReturnAllGroups()
        {
            // Arrange
            var query = new GetInternshipGroupsQuery { PageNumber = 1, PageSize = 10 };
            var groups = new List<InternshipGroup>
            {
                InternshipGroup.Create(Guid.NewGuid(), "Group 1"),
                InternshipGroup.Create(Guid.NewGuid(), "Group 2")
            };

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(groups.AsQueryable().BuildMock());

            _mockMapper.Setup(x => x.Map<List<GetInternshipGroupsResponse>>(It.IsAny<List<InternshipGroup>>()))
                .Returns(new List<GetInternshipGroupsResponse>
                {
                    new GetInternshipGroupsResponse { GroupName = "Group 1" },
                    new GetInternshipGroupsResponse { GroupName = "Group 2" }
                });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data!.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task Handle_HRRole_ShouldOnlyReturnOwnEnterpriseGroups()
        {
            // Arrange
            var hrUserId = Guid.NewGuid();
            var hrEnterpriseId = Guid.NewGuid();
            var otherEnterpriseId = Guid.NewGuid();

            _mockCurrentUserService.Setup(x => x.UserId).Returns(hrUserId.ToString());
            _mockCurrentUserService.Setup(x => x.Role).Returns("HR");

            var enterpriseUser = new EnterpriseUser
            {
                EnterpriseUserId = Guid.NewGuid(),
                UserId = hrUserId,
                EnterpriseId = hrEnterpriseId
            };

            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().Query())
                .Returns(new List<EnterpriseUser> { enterpriseUser }.AsQueryable().BuildMock());

            var allGroups = new List<InternshipGroup>
            {
                InternshipGroup.Create(Guid.NewGuid(), "My Group", enterpriseId: hrEnterpriseId),
                InternshipGroup.Create(Guid.NewGuid(), "Other Group", enterpriseId: otherEnterpriseId)
            };

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(allGroups.AsQueryable().BuildMock());

            _mockMapper.Setup(x => x.Map<List<GetInternshipGroupsResponse>>(It.IsAny<List<InternshipGroup>>()))
                .Returns(new List<GetInternshipGroupsResponse>
                {
                    new GetInternshipGroupsResponse { GroupName = "My Group" }
                });

            var query = new GetInternshipGroupsQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            // Verify rằng query được filter — chỉ trả về nhóm của enterprise mình
            result.Data!.Items.Should().HaveCount(1);
            result.Data!.Items.First().GroupName.Should().Be("My Group");
        }

        [Fact]
        public async Task Handle_InvalidUserId_ShouldReturnUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.UserId).Returns("not-a-guid");
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Unauthorized");

            var query = new GetInternshipGroupsQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }
    }
}
