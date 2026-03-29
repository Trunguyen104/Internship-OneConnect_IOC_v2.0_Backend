using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Jobs.Queries.GetJobs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;
using MockQueryable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IOCv2.Tests.Features.Jobs.Queries
{
    public class GetJobsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly IMapper _mapper;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<GetJobsHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly GetJobsHandler _handler;

        public GetJobsHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<GetJobsHandler>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            // Configure AutoMapper with the same mapping used by application
            var cfg = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Job, GetJobsResponse>()
                    .ForMember(d => d.CompanyName, opt => opt.MapFrom(s => s.Enterprise != null ? s.Enterprise.Name : string.Empty))
                    .ForMember(d => d.CompanyLogoUrl, opt => opt.MapFrom(s => s.Enterprise != null ? s.Enterprise.LogoUrl : null))
                    .ForMember(d => d.Status, opt => opt.MapFrom(s => (short? ) (s.Status.HasValue ? (short)s.Status.Value : 0)))
                    .ForMember(d => d.Quantity, opt => opt.MapFrom(s => s.Quantity))
                    .ForMember(d => d.ApplicationCount, opt => opt.MapFrom(s => s.InternshipApplications != null ? s.InternshipApplications.Count : 0))
                    .ForMember(d => d.IsDeleted, opt => opt.MapFrom(s => s.Status == JobStatus.DELETED))
                    // TermName mapping simplified to avoid deep navigation null issues in tests
                    .ForMember(d => d.TermName, opt => opt.MapFrom(s => string.Empty));
            });

            _mapper = cfg.CreateMapper();

            _handler = new GetJobsHandler(
                _mockUnitOfWork.Object,
                _mapper,
                _mockMessageService.Object,
                _mockLogger.Object,
                _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_EnterpriseUser_ShouldReturnOnlyEnterpriseJobs()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();

            _mockCurrentUserService.SetupGet(x => x.UserId).Returns(userId.ToString());
            _mockCurrentUserService.SetupGet(x => x.Role).Returns("HR"); // Enterprise role

            var enterpriseUser = new EnterpriseUser
            {
                EnterpriseUserId = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                UserId = userId
            };

            var jobForEnterprise = Job.Create(enterpriseId, "Title A", expireDate: DateTime.UtcNow.AddDays(10));
            jobForEnterprise.Enterprise = new Enterprise { EnterpriseId = enterpriseId, Name = "Acme Co", LogoUrl = "logo.png" };
            jobForEnterprise.InternshipApplications = new List<InternshipApplication>
            {
                new InternshipApplication()
            };

            var jobOtherEnterprise = Job.Create(Guid.NewGuid(), "Other Title", expireDate: DateTime.UtcNow.AddDays(5));
            jobOtherEnterprise.Enterprise = new Enterprise { EnterpriseId = Guid.NewGuid(), Name = "Other Co", LogoUrl = null };

            var jobs = new List<Job> { jobForEnterprise, jobOtherEnterprise };

            // Repository setups
            _mockUnitOfWork.Setup(x => x.Repository<Job>().Query()).Returns(jobs.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().Query()).Returns(new List<EnterpriseUser> { enterpriseUser }.AsQueryable().BuildMock());
            // University / Student repos not used for enterprise path but provide empty sets to be safe
            _mockUnitOfWork.Setup(x => x.Repository<UniversityUser>().Query()).Returns(new List<UniversityUser>().AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<Student>().Query()).Returns(new List<Student>().AsQueryable().BuildMock());

            var query = new GetJobsQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalCount.Should().Be(1);
            result.Data.Items.Should().HaveCount(1);
            result.Data.Items.First().CompanyName.Should().Be("Acme Co");
            result.Data.Items.First().Title.Should().Be(jobForEnterprise.Title);
        }

        [Fact]
        public async Task Handle_StudentAlreadyPlaced_ShouldReturnEmptyPage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockCurrentUserService.SetupGet(x => x.UserId).Returns(userId.ToString());
            _mockCurrentUserService.SetupGet(x => x.Role).Returns("Student");

            var uniUser = new UniversityUser
            {
                UniversityUserId = Guid.NewGuid(),
                UniversityId = Guid.NewGuid(),
                UserId = userId
            };

            var student = new Student
            {
                StudentId = Guid.NewGuid(),
                UserId = userId,
                InternshipStatus = StudentStatus.INTERNSHIP_IN_PROGRESS
            };

            // Repositories used by student path
            _mockUnitOfWork.Setup(x => x.Repository<Job>().Query()).Returns(new List<Job>().AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<UniversityUser>().Query()).Returns(new List<UniversityUser> { uniUser }.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<Student>().Query()).Returns(new List<Student> { student }.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().Query()).Returns(new List<EnterpriseUser>().AsQueryable().BuildMock());

            var query = new GetJobsQuery { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalCount.Should().Be(0);
            result.Data.Items.Should().BeEmpty();
        }
    }
}