namespace IOCv2.Application.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends a password reset email to the specified email address
        /// </summary>
        /// <param name="email">Recipient email address</param>
        /// <param name="resetLink">Password reset link</param>
        /// <param name="employeeName">Name of the employee</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if email sent successfully, false otherwise</returns>
        Task<bool> SendPasswordResetEmailAsync(
            string email,
            string resetLink,
            string employeeName,
            CancellationToken cancellationToken = default);

        Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);

        /// <summary>
        /// Sends an account creation email to a new employee with their login credentials
        /// </summary>
        /// <param name="email">Recipient email address</param>
        /// <param name="employeeName">Full name of the employee</param>
        /// <param name="email">Email for login</param>
        /// <param name="role">Employee role</param>
        /// <param name="password">Temporary password</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if email sent successfully, false otherwise</returns>
        Task<bool> SendAccountCreationEmailAsync(
            string email,
            string employeeName,
            string loginEmail,
            string role,
            string password,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a role change confirmation email to an employee
        /// </summary>
        /// <param name="email">Recipient email address</param>
        /// <param name="employeeName">Full name of the employee</param>
        /// <param name="oldEmployeeCode">Previous employee code</param>
        /// <param name="newEmployeeCode">New employee code</param>
        /// <param name="oldRole">Previous role</param>
        /// <param name="newRole">New role</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if email sent successfully, false otherwise</returns>
        Task<bool> SendRoleChangeConfirmationEmailAsync(
            string email,
            string fullName,
            string oldUserCode,
            string newUserCode,
            string oldRole,
            string newRole,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="employeeName"></param>
        /// <param name="loginEmail"></param>
        /// <param name="newPassword"></param>
        /// <param name="managerName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> SendPasswordResetByManagerEmailAsync(
            string email,
            string employeeName,
            string loginEmail,
            string newPassword,
            string managerName,
            CancellationToken cancellationToken = default);
    }
}
