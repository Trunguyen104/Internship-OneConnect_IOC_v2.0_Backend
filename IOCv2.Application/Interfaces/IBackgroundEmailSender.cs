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
            string employeeName,
            string employeeCode,
            string role,
            string password,
            Guid? auditTargetId = null,
            Guid? performedByEmployeeId = null,
            CancellationToken cancellationToken = default);

        ValueTask EnqueueRoleChangeEmailAsync(
            string email,
            string employeeName,
            string oldEmployeeCode,
            string newEmployeeCode,
            string oldRole,
            string newRole,
            Guid? auditTargetId = null,
            Guid? performedByEmployeeId = null,
            CancellationToken cancellationToken = default);

        ValueTask EnqueuePasswordResetByManagerEmailAsync(
            string email,
            string employeeName,
            string employeeCode,
            string newPassword,
            string managerName,
            Guid? auditTargetId = null,
            Guid? performedByEmployeeId = null,
            CancellationToken cancellationToken = default);

        ValueTask EnqueuePasswordResetEmailAsync(
       string email,
       string resetLink,
       string employeeName,
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
