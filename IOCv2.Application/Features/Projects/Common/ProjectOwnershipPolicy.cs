using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System.Linq.Expressions;

namespace IOCv2.Application.Features.Projects.Common
{
    public static class ProjectOwnershipPolicy
    {
        /// <summary>
        /// Ownership rule:
        /// - Unstarted project: owned by created mentor (Project.MentorId).
        /// - Assigned project (Active/Completed/Archived): owned by current mentor of linked group.
        /// </summary>
        public static bool CanManage(Project project, Guid mentorEnterpriseUserId)
        {
            if (project.OperationalStatus == OperationalStatus.Unstarted)
                return project.MentorId == mentorEnterpriseUserId;

            return project.InternshipGroup?.MentorId == mentorEnterpriseUserId;
        }

        public static Expression<Func<Project, bool>> BuildMentorVisibility(Guid mentorEnterpriseUserId)
        {
            return p =>
                (p.OperationalStatus == OperationalStatus.Unstarted && p.MentorId == mentorEnterpriseUserId) ||
                (p.OperationalStatus != OperationalStatus.Unstarted &&
                 p.InternshipGroup != null &&
                 p.InternshipGroup.MentorId == mentorEnterpriseUserId);
        }
    }
}

