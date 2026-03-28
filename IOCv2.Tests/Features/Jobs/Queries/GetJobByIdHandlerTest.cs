using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Jobs.Queries.GetJobById;
using IOCv2.Application.Features.Jobs.Queries.GetJobById.DTOs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using IOCv2.Application.Common.Models;
using MockQueryable;

namespace IOCv2.Tests.Features.Jobs.Queries
{
    public class GetJobByIdHandlerTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<GetJobByIdHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly IMapper _mapper;
        private readonly GetJobByIdHandler _handler;

        public GetJobByIdHandlerTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<GetJobByIdHandler>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            // Configure AutoMapper mapping used by handler (explicit mapping mirrors DTO.Mapping)
            var cfg = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Job, GetJobByIdResponse>()
                    .ForMember(dest => dest.JobId, opt => opt.MapFrom(src => src.JobId))
                    .ForMember(dest => dest.EnterpriseName, opt => opt.MapFrom(src => src.Enterprise != null ? src.Enterprise.Name : string.Empty))
                    .ForMember(dest => dest.Universities, opt => opt.MapFrom(src => src.Universities.Select(u => new UniversityDto { UniversityId = u.UniversityId, Name = u.Name })))
                    .ForMember(dest => dest.ApplicationStatusCounts, opt => opt.Ignore())
                    .ForMember(dest => dest.PlacedCount, opt => opt.Ignore())
                    .ForMember(dest => dest.FilledBanner, opt => opt.Ignore());
            });
            _mapper = cfg.CreateMapper();

            _handler = new GetJobByIdHandler(
                _mockUnitOfWork.Object,
                _mapper,
                _mockMessageService.Object,
                _mockLogger.Object,
                _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_JobNotFound_ReturnsNotFound()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            _mockCurrentUserService.SetupGet(x => x.UserId).Returns(Guid.NewGuid().ToString());
            _mockCurrentUserService.SetupGet(x => x.Role).Returns("Student");

            _mockUnitOfWork.Setup(x => x.Repository<Job>().Query()).Returns(new List<Job>().AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<UniversityUser>().Query()).Returns(new List<UniversityUser>().AsQueryable().BuildMock());
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Common.RecordNotFound)).Returns("Record not found");

            var query = new GetJobByIdQuery { JobId = jobId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            result.Error.Should().Be("Record not found");
        }

        [Fact]
        public async Task Handle_EnterpriseUserNotAssociated_ReturnsForbidden()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var job = Job.Create(Guid.NewGuid(), "Some Job");
            job.JobId = jobId;
            job.Enterprise = new Enterprise { EnterpriseId = Guid.NewGuid(), Name = "Acme" };

            var userId = Guid.NewGuid();
            _mockCurrentUserService.SetupGet(x => x.UserId).Returns(userId.ToString());
            _mockCurrentUserService.SetupGet(x => x.Role).Returns("HR");

            // EnterpriseUser repo returns none so user not associated
            _mockUnitOfWork.Setup(x => x.Repository<Job>().Query()).Returns(new List<Job> { job }.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().Query()).Returns(new List<EnterpriseUser>().AsQueryable().BuildMock());

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Common.Forbidden)).Returns("Forbidden");

            var query = new GetJobByIdQuery { JobId = jobId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
            result.Error.Should().Be("Forbidden");
        }

        [Fact]
        public async Task Handle_EnterpriseUser_WhenPlacedEqualsQuantity_ShouldReturnFilledBanner()
        {
            // Arrange
            var enterpriseId = Guid.NewGuid();
            var jobId = Guid.NewGuid();

            var job = Job.Create(enterpriseId, "Placement Job");
            job.JobId = jobId;
            job.Quantity = 1;
            job.EnterpriseId = enterpriseId;
            job.Enterprise = new Enterprise { EnterpriseId = enterpriseId, Name = "Acme Co" };

            var application = new InternshipApplication
            {
                ApplicationId = Guid.NewGuid(),
                JobId = jobId,
                Status = InternshipApplicationStatus.Placed
            };
            job.InternshipApplications = new List<InternshipApplication> { application };

            var entUser = new EnterpriseUser
            {
                EnterpriseUserId = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                UserId = Guid.NewGuid()
            };

            _mockCurrentUserService.SetupGet(x => x.UserId).Returns(entUser.UserId.ToString());
            _mockCurrentUserService.SetupGet(x => x.Role).Returns("HR");

            _mockUnitOfWork.Setup(x => x.Repository<Job>().Query()).Returns(new List<Job> { job }.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().Query()).Returns(new List<EnterpriseUser> { entUser }.AsQueryable().BuildMock());

            var banner = "Filled: 1/1";
            _mockMessageService.Setup(m => m.GetMessage("Job.Banner.Filled", It.IsAny<object[]>())).Returns(banner);

            var query = new GetJobByIdQuery { JobId = jobId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.PlacedCount.Should().Be(1);
            result.Data.FilledBanner.Should().Be(banner);
            // also check the mapping of enterprise name occurred
            result.Data.EnterpriseName.Should().Be("Acme Co");
        }
    }
}
