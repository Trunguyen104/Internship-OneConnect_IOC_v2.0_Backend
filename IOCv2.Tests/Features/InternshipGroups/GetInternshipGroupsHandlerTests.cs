using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IOCv2.Tests.Features.InternshipGroups
{
    public class GetInternshipGroupsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly GetInternshipGroupsHandler _handler;

        public GetInternshipGroupsHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();

            _handler = new GetInternshipGroupsHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object);
        }

        [Fact]
        public async Task Handle_ValidQuery_ShouldReturnPaginatedSuccess()
        {
            // Arrange
            var query = new GetInternshipGroupsQuery { PageNumber = 1, PageSize = 10 };
            var groups = new List<InternshipGroup>
            {
                InternshipGroup.Create(Guid.NewGuid(), "Group 1"),
                InternshipGroup.Create(Guid.NewGuid(), "Group 2")
            };

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(groups.AsQueryable());

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
            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }
    }
}
