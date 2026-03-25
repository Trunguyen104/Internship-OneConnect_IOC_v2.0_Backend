using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;

namespace IOCv2.Application.Features.Jobs.Queries.GetAllJobApplications
{
    public class GetJobApplicationResponse : IMapFrom<JobApplication>
    {
        public Guid ApplicationId { get; set; }
        public Guid JobId { get; set; }
        public string JobTitle { get; set; } = null!;
        public short JobStatus { get; set; } // job status short: Draft/Published/Closed/Deleted
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public string? StudentEmail { get; set; }
        public short Status { get; set; } // application status
        public DateTime AppliedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<JobApplication, GetJobApplicationResponse>()
                .ForMember(d => d.JobTitle, opt => opt.MapFrom(s => s.Job.Title))
                .ForMember(d => d.JobStatus, opt => opt.MapFrom(s => (short)s.Job.Status))
                .ForMember(d => d.StudentName, opt => opt.MapFrom(s => s.Student.User.FullName))
                .ForMember(d => d.StudentEmail, opt => opt.MapFrom(s => s.Student.User.Email))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => (short)s.Status));
        }
    }
}