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

        /// <summary>
        /// Sends a notification email to a university's contact email when the university is added to the system
        /// </summary>
        /// <param name="email">Contact email of the university</param>
        /// <param name="universityName">Name of the university</param>
        /// <param name="universityCode">Official code of the university</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if email sent successfully, false otherwise</returns>
        Task<bool> SendUniversityCreationEmailAsync(
            string email,
            string universityName,
            string universityCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a notification email to an enterprise's contact email when the enterprise is added to the system
        /// </summary>
        /// <param name="email">Contact email of the enterprise</param>
        /// <param name="enterpriseName">Name of the enterprise</param>
        /// <param name="taxCode">Tax code of the enterprise</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if email sent successfully, false otherwise</returns>
        Task<bool> SendEnterpriseCreationEmailAsync(
            string email,
            string enterpriseName,
            string taxCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a reservation notification email to the system administrator when someone fills the form on the landing page.
        /// </summary>
        Task<bool> SendLandingReservationEmailAsync(
            string partnerType,
            string partnerName,
            string email,
            string phone,
            string area,
            string hiringCount,
            string consultationDate,
            string selectedTime,
            string note,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a 6-digit OTP to the address for landing-page email verification.
        /// </summary>
        Task<bool> SendVerificationOtpEmailAsync(
            string email,
            string otpCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns true if the email domain has at least one MX record (mail exchanger).
        /// </summary>
        bool VerifyEmailMxRecordSync(string email);
    }
}
