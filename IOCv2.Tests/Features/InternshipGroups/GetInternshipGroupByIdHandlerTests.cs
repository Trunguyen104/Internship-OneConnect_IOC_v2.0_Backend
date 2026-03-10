using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroupById;
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
    public class GetInternshipGroupByIdHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<GetInternshipGroupByIdHandler>> _mockLogger;
        private readonly GetInternshipGroupByIdHandler _handler;

        public GetInternshipGroupByIdHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<GetInternshipGroupByIdHandler>>();
 
            _handler = new GetInternshipGroupByIdHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockMessageService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ExistingId_ShouldReturnSuccess()
        {
            // Arrange
            var group = InternshipGroup.Create(Guid.NewGuid(), "Test Group");
            var internshipId = group.InternshipId;
            var query = new GetInternshipGroupByIdQuery(internshipId);

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());

            _mockMapper.Setup(x => x.Map<GetInternshipGroupByIdResponse>(It.IsAny<InternshipGroup>()))
                .Returns(new GetInternshipGroupByIdResponse { InternshipId = internshipId, GroupName = "Test Group" });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue($"Error: {result.Message}");
            result.Data!.InternshipId.Should().Be(internshipId);
        }

        [Fact]
        public async Task Handle_NonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var query = new GetInternshipGroupByIdQuery(Guid.NewGuid());

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup>().AsQueryable().BuildMock());

            _mockMessageService.Setup(x => x.GetMessage(MessageKeys.Common.NotFound))
                .Returns("Not Found");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }
    }
}
