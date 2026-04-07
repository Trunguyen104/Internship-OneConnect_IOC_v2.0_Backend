namespace IOCv2.Application.Interfaces
{
    public interface IBackgroundEmailSender
    {
        ValueTask EnqueueEmailAsync(
            string recipientEmail,
            string subject,
            string body,
            Guid? auditTargetId = null,
            Guid? performedByEmployeeId = null,
            CancellationToken cancellationToken = default);

        ValueTask EnqueueAccountCreationEmailAsync(
            string email,
            string fullname,
            string loginEmail,
            string role,
            string password,
            Guid? auditTargetId = null,
            Guid? performedById = null,
            CancellationToken cancellationToken = default);

        ValueTask EnqueueRoleChangeEmailAsync(
            string email,
            string oldUserCode,
            string newUserCode,
            string oldRole,
            string newRole,
            Guid? auditTargetId = null,
            Guid? performedByEmployeeId = null,
            CancellationToken cancellationToken = default);

        ValueTask EnqueuePasswordResetBySuperAdminEmailAsync(
            string email,
            string fullname,
            string newPassword,
            string superAdminName,
            Guid? auditTargetId = null,
            Guid? performedByEmployeeId = null,
            CancellationToken cancellationToken = default);

        ValueTask EnqueuePasswordResetEmailAsync(
       string email,
       string resetLink,
       string fullname,
       Guid? auditTargetId = null,
       Guid? performedByEmployeeId = null,
       CancellationToken cancellationToken = default);

        ValueTask EnqueueUniversityCreationEmailAsync(
            string email,
            string universityName,
            string universityCode,
            Guid? auditTargetId = null,
            Guid? performedByEmployeeId = null,
            CancellationToken cancellationToken = default);

        ValueTask EnqueueEnterpriseCreationEmailAsync(
            string email,
            string enterpriseName,
            string taxCode,
            Guid? auditTargetId = null,
            Guid? performedByEmployeeId = null,
            CancellationToken cancellationToken = default);
    }

    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        // Context for Audit Log on failure
        public Guid? AuditTargetId { get; set; }
        public Guid? PerformedByEmployeeId { get; set; }
    }
}
