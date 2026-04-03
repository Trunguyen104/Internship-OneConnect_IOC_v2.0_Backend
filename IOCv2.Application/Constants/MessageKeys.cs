namespace IOCv2.Application.Constants
{
    public static class MessageKeys
    {
        public static class Common
        {
            public const string DatabaseConflict = "Common.DatabaseConflict";
            public const string DatabaseUpdateError = "Common.DatabaseUpdateError";
            public const string OperationCancelled = "Common.OperationCancelled";
            public const string Unauthorized = "Common.Unauthorized";
            public const string Forbidden = "Common.Forbidden";
            public const string NoPermission = "Common.NoPermission";
            public const string AccessDenied = "Common.AccessDenied";
            public const string NotFound = "Common.NotFound";
            public const string RecordNotFound = "Common.RecordNotFound";
            public const string InvalidRequest = "Common.InvalidRequest";
            public const string IdMismatch = "Common.IdMismatch";
            public const string InternalError = "Common.InternalError";
            public const string PageNumberInvalid = "Common.PageNumberInvalid";
            public const string PageSizeInvalid = "Common.PageSizeInvalid";
            public const string PageSizeTooLarge = "Common.PageSizeTooLarge";
        }

        public static class Password
        {
            public const string MinLength = "Password.MinLength";
            public const string RequireUppercase = "Password.RequireUppercase";
            public const string RequireLowercase = "Password.RequireLowercase";
            public const string RequireDigit = "Password.RequireDigit";
            public const string RequireSpecial = "Password.RequireSpecial";
            public const string NotEmpty = "Password.NotEmpty";
            public const string ConfirmationMismatch = "Password.ConfirmationMismatch";
            public const string MustBeDifferent = "Password.MustBeDifferent";
            public const string IncorrectCurrent = "Password.IncorrectCurrent";
        }

        public static class ResetPassword
        {
            public const string OnlyManagerCanReset = "ResetPassword.OnlyManagerCanReset";
            public const string OnlyActiveEmployeeCanReset = "ResetPassword.OnlyActiveEmployeeCanReset";
            public const string ReasonRequired = "ResetPassword.ReasonRequired";
            public const string ReasonMinLength = "ResetPassword.ReasonMinLength";
            public const string ReasonMaxLength = "ResetPassword.ReasonMaxLength";
            public const string SuccessWithEmail = "ResetPassword.SuccessWithEmail";
            public const string SuccessNoEmail = "ResetPassword.SuccessNoEmail";
            public const string StatusNoEmail = "ResetPassword.StatusNoEmail";
        }

        public static class Auth
        {
            public const string InvalidCredentials = "Auth.InvalidCredentials";
            public const string AccountInactive = "Auth.AccountInactive";
            public const string AccountBlocked = "Auth.AccountBlocked";
            public const string InvalidToken = "Auth.InvalidToken";
            public const string RefreshTokenNotFound = "Auth.RefreshTokenNotFound";
            public const string RefreshTokenExpired = "Auth.RefreshTokenExpired";
            public const string RefreshTokenRevoked = "Auth.RefreshTokenRevoked";
            public const string AccountCreationEmailFailed = "Auth.AccountCreationEmailFailed";
            public const string ResetRequestLimit = "Auth.ResetRequestLimit";
            public const string InvalidResetLink = "Auth.InvalidResetLink";
            public const string UserNotLoggedIn = "Auth.UserNotLoggedIn";
            public const string InvalidAction = "Auth.InvalidAction";
            public const string TooManyAttempts = "Auth.TooManyAttempts";
            public const string PasswordChangedSuccess = "Auth.PasswordChangedSuccess";
            public const string PasswordResetSuccess = "Auth.PasswordResetSuccess";
            public const string PasswordResetGenericMessage = "Auth.PasswordResetGenericMessage";
            public const string TokenRequired = "Auth.TokenRequired";
            public const string NewPasswordRequired = "Auth.NewPasswordRequired";
            public const string ConfirmPasswordRequired = "Auth.ConfirmPasswordRequired";
            public const string ConfirmPasswordMismatch = "Auth.ConfirmPasswordMismatch";
            public const string EmailRequired = "Auth.EmailRequired";
            public const string EmailInvalidFormat = "Auth.EmailInvalidFormat";
        }

        public static class Users
        {
            public const string NotFound = "Users.NotFound";
            public const string EmailConflict = "Users.EmailConflict";
            public const string CodeConflict = "Users.CodeConflict";
            public const string NotActive = "Users.NotActive";
            public const string CannotUpdateInactive = "Users.CannotUpdateInactive";
            public const string InvalidAuditor = "Users.InvalidAuditor";
            public const string RoleMismatch = "Users.RoleMismatch";
            public const string NewRoleMustBeDifferent = "Users.NewRoleMustBeDifferent";
            public const string RoleChangedButEmailFailed = "Users.RoleChangedButEmailFailed";
            public const string CodeInvalidFormat = "Users.CodeInvalidFormat";
        }

        public static class University
        {
            public const string NotFound = "University.NotFound";
            public const string DuplicateCode = "University.DuplicateCode";
            public const string Inactive = "University.Inactive";
        }

        public static class Enterprise
        {
            public const string NotFound = "Enterprise.NotFound";
            public const string LogNotFound = "Enterprise.LogNotFound";
            public const string DuplicateTaxCode = "Enterprise.DuplicateTaxCode";
            public const string Unverified = "Enterprise.Unverified";
            public const string Inactive = "Enterprise.Inactive";
            public const string GetEnterprisesError = "Enterprise.GetEnterprisesError";
            public const string GetEnterpriseByIdError = "Enterprise.GetEnterpriseByIdError";
            public const string EnterpriseWithSameTaxCodeExists = "Enterprise.EnterpriseWithSameTaxCodeExists";
            public const string LogEnterpriseWithSameTaxCodeExists = "Enterprise.LogEnterpriseWithSameTaxCodeExists";
            public const string ErrorCreatingEnterprise = "Enterprise.ErrorCreatingEnterprise";
            public const string WebsiteNotValid = "Enterprise.WebsiteNotValid";
            public const string logoUrlNotValid = "Enterprise.LogoUrlNotValid";
            public const string BackgroundUrlNotValid = "Enterprise.BackgroundUrlNotValid";
            public const string UpdatePermissionsNotAllowed = "Enterprise.UpdatePermissionsNotAllowed";
            public const string UpdateTaxCodeNotAllowed = "Enterprise.UpdateTaxCodeNotAllowed";
            public const string LogUpdatePermissionsNotAllowed = "Enterprise.LogUpdatePermissionsNotAllowed";
            public const string LogUpdateEnterpriseError = "Enterprise.LogUpdateEnterpriseError";
            public const string LogDeleteError = "Enterprise.LogDeleteError";
            public const string DeleteError = "Enterprise.DeleteError";
            public const string RequestManyTimes = "Enterprise.RequestManyTimes";
            public const string RateLimitUpdateAttempt = "Enterprise.RateLimitUpdateAttempt";
            public const string RateLimitGetByHRAttempt = "Enterprise.RateLimitGetByHRAttempt";
            public const string RateLimitGetByIDAttempt = "Enterprise.RateLimitGetByIDAttempt";
            public const string RateLimitGetEnterprisesAttempt = "Enterprise.RateLimitGetEnterprisesAttempt";
            public const string HRNotAssociatedWithEnterprise = "Enterprise.HR.NotAssociatedWithEnterprise";
            public const string EnterpriseNotFoundCurrentHR = "Enterprise.NotFoundCurrentHR";
            public const string DeletePermissionDenied = "Enterprise.DeletePermissionDenied";
            // Restore
            public const string RestorePermissionDenied = "Enterprise.RestorePermissionDenied";
            public const string NotDeleted = "Enterprise.NotDeleted";
            public const string LogRestoreFailed = "Enterprise.LogRestoreFailed";
            public const string RestoreFailed = "Enterprise.RestoreFailed";
            // GetByID
            public const string GetByIDPermissionsNotAllowed = "Enterprise.GetByIDPermissionsNotAllowed";

        }

        public static class Profile
        {
            public const string PhoneExists = "Profile.PhoneExists";
            public const string EmailExists = "Profile.EmailExists";
            public const string UserIdRequired = "Profile.UserIdRequired";
            public const string FullNameRequired = "Profile.FullNameRequired";
            public const string FullNameMaxLength = "Profile.FullNameMaxLength";
            public const string EmailRequired = "Profile.EmailRequired";
            public const string EmailInvalid = "Profile.EmailInvalid";
            public const string PhoneRequired = "Profile.PhoneRequired";
            public const string PhoneInvalid = "Profile.PhoneInvalid";
            
            // New Validation Rules
            public const string PhoneMaxLength = "Profile.PhoneMaxLength";
            public const string PortfolioUrlMaxLength = "Profile.PortfolioUrlMaxLength";
            public const string PortfolioUrlInvalid = "Profile.PortfolioUrlInvalid";
            public const string BioMaxLength = "Profile.BioMaxLength";
            public const string ExpertiseMaxLength = "Profile.ExpertiseMaxLength";
            public const string DepartmentMaxLength = "Profile.DepartmentMaxLength";
            public const string CvFileMaxSize = "Profile.CvFileMaxSize";
            public const string CvFileInvalidFormat = "Profile.CvFileInvalidFormat";

            // Errors
            public const string StudentNotFound = "Profile.StudentNotFound";
            public const string CvNotFound = "Profile.CvNotFound";
            public const string FileNotFoundInStorage = "Profile.FileNotFoundInStorage";

            // Success
            public const string UpdateSuccess = "Profile.UpdateSuccess";

            // Logger
            public const string LogUpdateStart = "Profile.LogUpdateStart";
            public const string LogAuthFailed = "Profile.LogAuthFailed";
            public const string LogUserNotFound = "Profile.LogUserNotFound";
            public const string LogUpdateSuccess = "Profile.LogUpdateSuccess";
            public const string LogUpdateError = "Profile.LogUpdateError";
            public const string LogCvFileNotFound = "Profile.LogCvFileNotFound";
            public const string LogGetProfile = "Profile.LogGetProfile";
            public const string LogGetProfileUserNotFound = "Profile.LogGetProfileUserNotFound";
        }

        public static class InternshipGroups
        {
            public const string NameRequired = "InternshipGroup.NameRequired";
            public const string NameMaxLength = "InternshipGroup.NameMaxLength";
            public const string TermIdRequired = "InternshipGroup.TermIdRequired";
            public const string StartDateBeforeEndDate = "InternshipGroup.StartDateBeforeEndDate";
            public const string StudentListRequired = "InternshipGroup.StudentListRequired";
            public const string StudentListToRemoveRequired = "InternshipGroup.StudentListToRemoveRequired";
            public const string InvalidStudentId = "InternshipGroup.InvalidStudentId";
            public const string TermNotFound = "InternshipGroup.TermNotFound";
            public const string TermNotActive = "InternshipGroup.TermNotActive";
            public const string EnterpriseNotFound = "InternshipGroup.EnterpriseNotFound";
            public const string MentorNotFound = "InternshipGroup.MentorNotFound";
            public const string StudentNotFound = "InternshipGroup.StudentNotFound";
            public const string NotFound = "InternshipGroup.NotFound";
            public const string HasStudents = "InternshipGroup.HasStudents";
            public const string EnterpriseUserNotFound = "InternshipGroup.EnterpriseUserNotFound";
            public const string StudentNotApproved = "InternshipGroup.StudentNotApproved";
            public const string LogStudentNotApproved = "InternshipGroup.LogStudentNotApproved";
            public const string AtLeastOneStudentRequired = "InternshipGroup.AtLeastOneStudentRequired";
            public const string StudentAlreadyInActiveGroup = "InternshipGroup.StudentAlreadyInActiveGroup";

            // Move & Archive keys
            public const string MustBelongToYourEnterprise = "InternshipGroup.MustBelongToYourEnterprise";
            public const string MustBeInSameTerm = "InternshipGroup.MustBeInSameTerm";
            public const string MustBeActive = "InternshipGroup.MustBeActive";
            public const string StudentsNotInSourceGroup = "InternshipGroup.StudentsNotInSourceGroup";
            public const string MoveSuccess = "InternshipGroup.MoveSuccess";
            public const string ArchiveSuccess = "InternshipGroup.ArchiveSuccess";
            public const string GroupAlreadyArchived = "InternshipGroup.GroupAlreadyArchived";
            public const string CannotArchiveNoData = "InternshipGroup.CannotArchiveNoData";
            public const string HasActivityData = "InternshipGroup.HasActivityData";   // Nhóm có data thực tế, không thể xóa


            // Logger Keys
            public const string LogTermNotFound = "InternshipGroup.LogTermNotFound";
            public const string LogEnterpriseNotFound = "InternshipGroup.LogEnterpriseNotFound";
            public const string LogMentorNotFound = "InternshipGroup.LogMentorNotFound";
            public const string LogStudentNotFound = "InternshipGroup.LogStudentNotFound";
            public const string LogNotFound = "InternshipGroup.LogNotFound";

            public const string LogNoStudentsProvided = "InternshipGroup.LogNoStudentsProvided";
            public const string LogStudentAlreadyInActiveGroup = "InternshipGroup.LogStudentAlreadyInActiveGroup";

            public const string LogCreating = "InternshipGroup.LogCreating";
            public const string LogCreatedSuccess = "InternshipGroup.LogCreatedSuccess";
            public const string LogCreationFailed = "InternshipGroup.LogCreationFailed";
            public const string LogCreationError = "InternshipGroup.LogCreationError";

            public const string LogUpdating = "InternshipGroup.LogUpdating";
            public const string LogUpdatedSuccess = "InternshipGroup.LogUpdatedSuccess";
            public const string LogUpdateFailed = "InternshipGroup.LogUpdateFailed";
            public const string LogUpdateError = "InternshipGroup.LogUpdateError";

            public const string LogDeleting = "InternshipGroup.LogDeleting";
            public const string LogDeletedSuccess = "InternshipGroup.LogDeletedSuccess";
            public const string LogDeleteFailed = "InternshipGroup.LogDeleteFailed";
            public const string LogDeleteError = "InternshipGroup.LogDeleteError";

            public const string LogGroupNotActive                = "InternshipGroup.LogGroupNotActive";
            public const string LogPhaseNotOpen                  = "InternshipGroup.LogPhaseNotOpen";
            public const string LogUnauthorizedEnterpriseAccess  = "InternshipGroup.LogUnauthorizedEnterpriseAccess";
            public const string LogMentorRoleInvalid             = "InternshipGroup.LogMentorRoleInvalid";

            public const string LogAddingStudents = "InternshipGroup.LogAddingStudents";
            public const string LogInvalidStudentIds = "InternshipGroup.LogInvalidStudentIds";
            public const string LogAddedStudentsSuccess = "InternshipGroup.LogAddedStudentsSuccess";
            public const string LogAddStudentsFailed = "InternshipGroup.LogAddStudentsFailed";
            public const string LogAddStudentsError = "InternshipGroup.LogAddStudentsError";

            public const string LogRemovingStudents = "InternshipGroup.LogRemovingStudents";
            public const string LogRemovedStudentsSuccess = "InternshipGroup.LogRemovedStudentsSuccess";
            public const string LogRemoveStudentsFailed = "InternshipGroup.LogRemoveStudentsFailed";
            public const string LogRemoveStudentsError = "InternshipGroup.LogRemoveStudentsError";

            // Phase 4 side-effects
            public const string NotificationProjectCleanupTitle = "InternshipGroup.NotificationProjectCleanupTitle";
            public const string NotificationProjectCleanupContent = "InternshipGroup.NotificationProjectCleanupContent";
            public const string NotificationGroupArchivedTitle = "InternshipGroup.NotificationGroupArchivedTitle";
            public const string NotificationGroupArchivedContent = "InternshipGroup.NotificationGroupArchivedContent";
            public const string NotificationStudentRemovedMentorTitle = "InternshipGroup.NotificationStudentRemovedMentorTitle";
            public const string NotificationStudentRemovedMentorContent = "InternshipGroup.NotificationStudentRemovedMentorContent";
            public const string NotificationStudentRemovedStudentTitle = "InternshipGroup.NotificationStudentRemovedStudentTitle";
            public const string NotificationStudentRemovedStudentContent = "InternshipGroup.NotificationStudentRemovedStudentContent";
            public const string NotificationStudentMovedTitle = "InternshipGroup.NotificationStudentMovedTitle";
            public const string NotificationStudentMovedContent = "InternshipGroup.NotificationStudentMovedContent";
            public const string NotificationMentorOldGroupTitle = "InternshipGroup.NotificationMentorOldGroupTitle";
            public const string NotificationMentorOldGroupContent = "InternshipGroup.NotificationMentorOldGroupContent";
            public const string NotificationMentorNewGroupTitle = "InternshipGroup.NotificationMentorNewGroupTitle";
            public const string NotificationMentorNewGroupContent = "InternshipGroup.NotificationMentorNewGroupContent";
            public const string NotificationMentorReplacedOldTitle = "InternshipGroup.NotificationMentorReplacedOldTitle";
            public const string NotificationMentorReplacedOldContent = "InternshipGroup.NotificationMentorReplacedOldContent";
            public const string NotificationMentorReplacedNewTitle = "InternshipGroup.NotificationMentorReplacedNewTitle";
            public const string NotificationMentorReplacedNewContent = "InternshipGroup.NotificationMentorReplacedNewContent";
            public const string LogProjectAssignmentsCleanup = "InternshipGroup.LogProjectAssignmentsCleanup";
            public const string LogProjectsArchived = "InternshipGroup.LogProjectsArchived";
            public const string LogArchiveNotifyFailed = "InternshipGroup.LogArchiveNotifyFailed";
            public const string LogMoveNotificationFailed = "InternshipGroup.LogMoveNotificationFailed";
            public const string LogMentorSwapNotificationFailed = "InternshipGroup.LogMentorSwapNotificationFailed";
            public const string GroupNotActive = "InternshipGroup.GroupNotActive";

            // New US keys
            public const string CannotDeleteNotActive      = "InternshipGroups.CannotDeleteNotActive";
            public const string NotificationOrphanTitle    = "InternshipGroups.NotificationOrphanTitle";
            public const string NotificationOrphanContent  = "InternshipGroups.NotificationOrphanContent";
            public const string NotificationGroupDeletedStudentTitle = "InternshipGroups.NotificationGroupDeletedStudentTitle";
            public const string NotificationGroupDeletedStudentContent = "InternshipGroups.NotificationGroupDeletedStudentContent";
            public const string LogOrphanizeProjects       = "InternshipGroups.LogOrphanizeProjects";

            // Log messages — GetInternshipGroupByIdHandler
            public const string LogQueryById                   = "InternshipGroup.LogQueryById";
            public const string LogAccessDeniedHrEnterprise    = "InternshipGroup.LogAccessDeniedHrEnterprise";
            public const string LogAccessDeniedMentor          = "InternshipGroup.LogAccessDeniedMentor";
            public const string LogAccessDeniedStudent         = "InternshipGroup.LogAccessDeniedStudent";

            // Log messages — DeleteInternshipGroupHandler
            public const string LogDeleteNotificationFailed    = "InternshipGroup.LogDeleteNotificationFailed";
            public const string LogDeleteStudentNotificationFailed = "InternshipGroup.LogDeleteStudentNotificationFailed";

            // Log messages — GetInternshipGroupsHandler
            public const string LogScopedHrEnterprise          = "InternshipGroup.LogScopedHrEnterprise";
            public const string LogScopedMentor                = "InternshipGroup.LogScopedMentor";
            public const string LogStudentNotFoundForUser      = "InternshipGroup.LogStudentNotFoundForUser";
            public const string LogScopedStudent               = "InternshipGroup.LogScopedStudent";
            public const string LogRetrievedGroups             = "InternshipGroup.LogRetrievedGroups";

            // Log messages — GetMyInternshipGroupsHandler
            public const string LogStartQueryMine              = "InternshipGroup.LogStartQueryMine";
            public const string LogQueryMineDenied             = "InternshipGroup.LogQueryMineDenied";
            public const string LogQueryMineStudentNotFound    = "InternshipGroup.LogQueryMineStudentNotFound";
            public const string LogQueryMineCompleted          = "InternshipGroup.LogQueryMineCompleted";

            // Log messages — GetMyInternshipTermsHandler
            public const string LogStartQueryTerms             = "InternshipGroup.LogStartQueryTerms";

            // Log messages — GetInternshipGroupDashboardHandler
            public const string LogDashboardQuery              = "InternshipGroup.LogDashboardQuery";
            public const string LogDashboardNotFound           = "InternshipGroup.LogDashboardNotFound";
            public const string LogDashboardGenerated          = "InternshipGroup.LogDashboardGenerated";

            // Log messages — GetPlacedStudentsHandler
            public const string LogNoPhasesForEnterprise       = "InternshipGroup.LogNoPhasesForEnterprise";
            public const string LogResolvedPhases              = "InternshipGroup.LogResolvedPhases";

            // Archive/Move error keys
            public const string LogArchiveError                = "InternshipGroup.LogArchiveError";
            public const string LogMoveError                   = "InternshipGroup.LogMoveError";

            // UI labels
            public const string Unassigned                    = "InternshipGroup.Unassigned";
            public const string LateLogbookSubmission         = "InternshipGroup.LateLogbookSubmission";

            // ── AssignMentorToGroup ───────────────────────────────────────────────
            public const string AssignMentorSuccess              = "InternshipGroup.AssignMentorSuccess";
            public const string AssignMentorGroupNotFound        = "InternshipGroup.AssignMentorGroupNotFound";
            public const string AssignMentorGroupNotActive       = "InternshipGroup.AssignMentorGroupNotActive";
            public const string AssignMentorGroupIdRequired      = "InternshipGroup.AssignMentorGroupIdRequired";
            public const string AssignMentorMentorRequired       = "InternshipGroup.AssignMentorMentorRequired";
            public const string AssignMentorSameMentor           = "InternshipGroup.AssignMentorSameMentor";
            public const string LogAssignMentorSuccess           = "InternshipGroup.Log.AssignMentorSuccess";
            public const string LogAssignMentorFailed            = "InternshipGroup.Log.AssignMentorFailed";
            public const string LogAssignMentorNotifyFailed      = "InternshipGroup.Log.AssignMentorNotifyFailed";
            public const string LogAssignMentorSameMentor        = "InternshipGroup.Log.AssignMentorSameMentor";

            // ── GetAvailableMentors ───────────────────────────────────────────────
            public const string AvailableMentorsRetrieved        = "InternshipGroup.AvailableMentorsRetrieved";
            public const string LogGetAvailableMentors           = "InternshipGroup.Log.GetAvailableMentors";
            public const string LogGetAvailableMentorsAccessDenied = "InternshipGroup.Log.GetAvailableMentorsAccessDenied";

            // ── AC-04: Notification — Gán Mentor lần đầu ─────────────────────────
            public const string NotificationMentorAssignedFirstTitle   = "InternshipGroup.NotificationMentorAssignedFirstTitle";
            public const string NotificationMentorAssignedFirstContent = "InternshipGroup.NotificationMentorAssignedFirstContent";
            public const string NotificationStudentMentorAssignedTitle   = "InternshipGroup.NotificationStudentMentorAssignedTitle";
            public const string NotificationStudentMentorAssignedContent = "InternshipGroup.NotificationStudentMentorAssignedContent";

            // ── AC-05: Notification — Sinh viên khi đổi Mentor ───────────────────
            public const string NotificationStudentMentorChangedTitle   = "InternshipGroup.NotificationStudentMentorChangedTitle";
            public const string NotificationStudentMentorChangedContent = "InternshipGroup.NotificationStudentMentorChangedContent";
        }

        public static class Terms
        {
            public const string NotFound = "Terms.NotFound";
            public const string UniversityIdRequired = "Terms.UniversityIdRequired";
            public const string NameRequired = "Terms.NameRequired";
            public const string NameMaxLength = "Terms.NameMaxLength";
            public const string NameContainsDangerousCharacters = "Terms.NameContainsDangerousCharacters";
            public const string NameExists = "Terms.NameExists";
            public const string StartDateRequired = "Terms.StartDateRequired";
            public const string EndDateRequired = "Terms.EndDateRequired";
            public const string EndDateMustBeAfterStart = "Terms.EndDateMustBeAfterStart";
            public const string StartDateInPast = "Terms.StartDateInPast";
            public const string StartDateMustBeOneWeekAhead = "Terms.StartDateMustBeOneWeekAhead";
            public const string InvalidDateFormat = "Terms.InvalidDateFormat";
            public const string OverlapWithActiveTerm = "Terms.OverlapWithActiveTerm";
            public const string CreateSuccess = "Terms.CreateSuccess";
            public const string UpdateSuccess = "Terms.UpdateSuccess";
            public const string CloseSuccess = "Terms.CloseSuccess";
            public const string DeleteSuccess = "Terms.DeleteSuccess";
            public const string OnlyActiveCanBeClosed = "Terms.OnlyActiveCanBeClosed";
            public const string OnlyUpcomingCanBeDeleted = "Terms.OnlyUpcomingCanBeDeleted";
            public const string VersionConflict = "Terms.VersionConflict";
            public const string StatusChanged = "Terms.StatusChanged";
            public const string AlreadyDeleted = "Terms.AlreadyDeleted";
            public const string UnplacedStudentsWarning = "Terms.UnplacedStudentsWarning";
            public const string HasRelatedData = "Terms.HasRelatedData";
            public const string StartDateLocked = "Terms.StartDateLocked";
            public const string YearInvalidRange = "Terms.YearInvalidRange";
            public const string CloseReasonRequired = "Terms.CloseReasonRequired";
            public const string CloseReasonMinLength = "Terms.CloseReasonMinLength";
            public const string CloseReasonMaxLength = "Terms.CloseReasonMaxLength";

            // Logging messages
            public const string LogTermCreated = "Terms.Log.TermCreated";
            public const string LogTermUpdated = "Terms.Log.TermUpdated";
            public const string LogTermClosed = "Terms.Log.TermClosed";
            public const string LogTermDeleted = "Terms.Log.TermDeleted";
            public const string LogTermsRetrieved = "Terms.Log.TermsRetrieved";
            public const string LogUserNotAssociatedWithUniversity = "Terms.Log.UserNotAssociatedWithUniversity";
            public const string LogTermNotFound = "Terms.Log.TermNotFound";
            public const string LogTermNotFoundOrAccessDenied = "Terms.Log.TermNotFoundOrAccessDenied";
            public const string LogVersionConflict = "Terms.Log.VersionConflict";
            public const string LogVersionConflictDetailed = "Terms.Log.VersionConflictDetailed";
            public const string LogDuplicateTermName = "Terms.Log.DuplicateTermName";
            public const string LogConcurrencyConflictUpdating = "Terms.Log.ConcurrencyConflictUpdating";
            public const string LogConcurrencyConflictClosing = "Terms.Log.ConcurrencyConflictClosing";
            public const string LogErrorCreatingTerm = "Terms.Log.ErrorCreatingTerm";
            public const string LogErrorUpdatingTerm = "Terms.Log.ErrorUpdatingTerm";
            public const string LogErrorClosingTerm = "Terms.Log.ErrorClosingTerm";
            public const string LogErrorDeletingTerm = "Terms.Log.ErrorDeletingTerm";
            public const string LogErrorRetrievingTerms = "Terms.Log.ErrorRetrievingTerms";
            public const string LogErrorRetrievingTerm = "Terms.Log.ErrorRetrievingTerm";
        }
        public static class Projects
        {
            public const string NotFound = "Projects.NotFound";
            public const string InvalidStatus = "Projects.InvalidStatus";
            public const string CannotDeleteActive = "Projects.CannotDeleteActive";
            public const string GetAllError = "Projects.GetAllError";
            public const string GetByStuIdEr = "Projects.GetByStudentIdError";
            public const string DeleteSuccess = "Projects.DeleteSuccess";
            public const string UpdateSuccess = "Projects.UpdateSuccess";
            public const string UpdateError = "Projects.UpdateError";
            public const string CreateSuccess = "Projects.CreateSuccess";
            public const string LogDelete = "Projects.LogDelete";
            public const string LogDeleteError = "Projects.LogDeleteError";
            public const string ProjectIdRequired = "Projects.ProjectIdRequired";
            public const string LogNotFound = "Projects.LogNotFound";
            public const string ProjectNameExistsInternship = "Projects.ProjectNameExistsInternship";
            public const string LogCreateSuccess = "Projects.LogCreateSuccess";
            public const string LogCreateError = "Projects.LogCreateError";
            public const string ProjectsInternshipIdRequired = "Projects.InternshipIdRequired";
            public const string ProjectsProjectNameRequired = "Projects.ProjectNameRequired";

            public const string ProjectNameMaxLength = "Projects.ProjectNameMaxLength";
            public const string DescriptionMaxLength = "Projects.DescriptionMaxLength";
            public const string StartDateInvalidRange = "Projects.StartDateInvalidRange";
            public const string EndDateInvalidRange = "Projects.EndDateInvalidRange";
            public const string LogGetByInternshipIdSuccess = "Projects.LogGetByInternshipIdSuccess";
            public const string LogGetByInternshipIdErr = "Projects.LogGetByInternshipIdErr";
            public const string LogGetByIdError = "Projects.LogGetByIdError";

            // Lifecycle success
            public const string PublishSuccess = "Projects.PublishSuccess";
            public const string CompleteSuccess = "Projects.CompleteSuccess";

            // Scope/Status errors
            public const string MentorNotFound = "Projects.MentorNotFound";
            public const string GroupNotActiveForCreate = "Projects.GroupNotActiveForCreate";
            public const string GroupNotActiveForPublish = "Projects.GroupNotActiveForPublish";
            public const string GroupNotActiveForUpdate = "Projects.GroupNotActiveForUpdate";
            public const string InvalidStatusForPublish = "Projects.InvalidStatusForPublish";
            public const string InvalidStatusForComplete = "Projects.InvalidStatusForComplete";
            public const string InvalidStatusForUpdate = "Projects.InvalidStatusForUpdate";
            public const string CannotDeleteWithActiveAssignments = "Projects.CannotDeleteWithActiveAssignments";
            public const string ProjectCodeConflict = "Projects.ProjectCodeConflict";

            // Validation
            public const string FieldRequired = "Projects.FieldRequired";
            public const string RequirementsRequired = "Projects.RequirementsRequired";
            public const string FieldMaxLength = "Projects.FieldMaxLength";
            public const string RequirementsMaxLength = "Projects.RequirementsMaxLength";
            public const string DeliverablesMaxLength = "Projects.DeliverablesMaxLength";

            // Log messages
            public const string LogPublishSuccess       = "Projects.LogPublishSuccess";
            public const string LogPublishError         = "Projects.LogPublishError";
            public const string LogCompleteSuccess      = "Projects.LogCompleteSuccess";
            public const string LogCompleteError        = "Projects.LogCompleteError";
            public const string LogUpdateSuccess        = "Projects.LogUpdateSuccess";
            public const string LogUpdateError          = "Projects.LogUpdateError";
            public const string LogDeleteHard           = "Projects.LogDeleteHard";
            public const string LogDeleteSoft           = "Projects.LogDeleteSoft";
            public const string LogCreating             = "Projects.LogCreating";
            public const string LogUpdating             = "Projects.LogUpdating";
            public const string LogDeleting             = "Projects.LogDeleting";
            public const string LogGetById              = "Projects.LogGetById";
            public const string LogNameExists           = "Projects.LogNameExists";
            public const string LogCodeConflict         = "Projects.LogCodeConflict";
            public const string LogGetAll               = "Projects.LogGetAll";
            public const string LogGetAllSuccess        = "Projects.LogGetAllSuccess";
            public const string LogGetByInternshipId    = "Projects.LogGetByInternshipId";

            // New US keys
            public const string NoGroupAssigned        = "Projects.NoGroupAssigned";
            public const string GroupIsFinished        = "Projects.GroupIsFinished";
            public const string CannotDeleteHasData    = "Projects.CannotDeleteHasData";
            public const string LogOrphanized          = "Projects.LogOrphanized";
            public const string GetStudentsSuccess     = "Projects.GetStudentsSuccess";
            public const string LogGetStudentsSuccess  = "Projects.LogGetStudentsSuccess";

            // Unpublish
            public const string UnpublishSuccess              = "Projects.UnpublishSuccess";
            public const string CannotUnpublishStarted        = "Projects.CannotUnpublishStarted";
            public const string InvalidStatusForUnpublish     = "Projects.InvalidStatusForUnpublish";
            public const string LogUnpublishSuccess           = "Projects.LogUnpublishSuccess";
            public const string LogUnpublishError             = "Projects.LogUnpublishError";

            // Archive
            public const string ArchiveSuccess             = "Projects.ArchiveSuccess";
            public const string MustBeCompletedToArchive   = "Projects.MustBeCompletedToArchive";
            public const string LogArchiveSuccess          = "Projects.LogArchiveSuccess";
            public const string LogArchiveError            = "Projects.LogArchiveError";

            // AssignGroup
            public const string AssignGroupSuccess         = "Projects.AssignGroupSuccess";
            public const string AlreadyAssignedToGroup     = "Projects.AlreadyAssignedToGroup";
            public const string GroupNotActive             = "Projects.GroupNotActive";
            public const string CannotAssignArchivedGroup  = "Projects.CannotAssignArchivedGroup";
            public const string GroupPhaseEnded            = "Projects.GroupPhaseEnded";
            public const string LogAssignGroupSuccess      = "Projects.LogAssignGroupSuccess";
            public const string LogAssignGroupError        = "Projects.LogAssignGroupError";

            // SwapGroup
            public const string SwapGroupSuccess           = "Projects.SwapGroupSuccess";
            public const string ProjectNotAssigned         = "Projects.ProjectNotAssigned";
            public const string HasStudentDataWorkItems    = "Projects.HasStudentDataWorkItems";
            public const string HasStudentDataSprints      = "Projects.HasStudentDataSprints";
            public const string LogSwapGroupSuccess        = "Projects.LogSwapGroupSuccess";
            public const string LogSwapGroupError          = "Projects.LogSwapGroupError";

            // Notification messages (stored in DB)
            public const string NotifNewProjectTitle       = "Projects.NotifNewProjectTitle";
            public const string NotifNewProjectContent     = "Projects.NotifNewProjectContent";
            public const string NotifProjectLeftTitle      = "Projects.NotifProjectLeftTitle";
            public const string NotifProjectLeftContent    = "Projects.NotifProjectLeftContent";
            public const string NotifUpdatedTitle          = "Projects.NotifUpdatedTitle";
            public const string NotifUpdatedContent        = "Projects.NotifUpdatedContent";
            public const string NotifCompletedTitle        = "Projects.NotifCompletedTitle";
            public const string NotifCompletedContent      = "Projects.NotifCompletedContent";

            // Log messages for notification failures
            public const string LogCompleteNotificationFailed  = "Projects.LogCompleteNotificationFailed";
            public const string LogAssignNotificationFailed    = "Projects.LogAssignNotificationFailed";
            public const string LogSwapNotificationFailed      = "Projects.LogSwapNotificationFailed";
            public const string LogUpdateNotificationFailed    = "Projects.LogUpdateNotificationFailed";

            // Log messages for resource cleanup failures
            public const string LogCleanupResourceFailed       = "Projects.LogCleanupResourceFailed";
            public const string LogDeleteResourceAfterUpdate   = "Projects.LogDeleteResourceAfterUpdate";

            // AC-13: SignalR ProjectListChanged log
            public const string LogProjectListChanged      = "Projects.LogProjectListChanged";
        }


        public static class ProjectResourcesKey
        {
            public const string NotFound = "ProjectResources.NotFound";
            public const string LogUploadSuccess = "ProjectResources.LogUploadSuccess";
            public const string LogUploadError = "ProjectResources.LogUploadError";
            public const string UpdateSuccess = "ProjectResources.UpdateSuccess";
            public const string LogDeleteSuccess = "ProjectResources.LogDeleteSuccess";
            public const string ProjectIdRequired = "ProjectResources.ProjectIdRequired";
            public const string ResourceNameMaxLength = "ProjectResources.ResourceNameMaxLength";
            public const string LogProjectResourceNotFound = "ProjectResources.LogNotFound";
            public const string LogUpdateError = "ProjectResources.LogUpdateError";
            public const string UpdateError = "ProjectResources.UpdateError";
            public const string FileRequired = "ProjectResources.FileRequired";
            public const string InvalidFileType = "ProjectResources.InvalidFileType";
            public const string FileSizeExceeded = "ProjectResources.FileSizeExceeded";
            public const string GetAllSuccess = "ProjectResources.GetAllSuccess";
            public const string GetAllError = "ProjectResources.GetAllError";
            public const string GetByIdSuccess = "ProjectResources.GetByIdSuccess";
            public const string GetByIdError = "ProjectResources.GetByIdError";

            public const string LogUploadAutoSetFileTypeError = "ProjectResources.LogUploadAutoSetFileTypeError";
            public const string FileOrLinkRequired = "ProjectResources.FileOrLinkRequired";
            public const string FileAndLinkMutuallyExclusive = "ProjectResources.FileAndLinkMutuallyExclusive";
            public const string InvalidExternalUrl = "ProjectResources.InvalidExternalUrl";
            public const string LinkTypeRequired = "ProjectResources.LinkTypeRequired";
            public const string LinkDownloadNotSupported = "ProjectResources.LinkDownloadNotSupported";
            public const string StudentCannotModifyMentorResource = "ProjectResources.StudentCannotModifyMentorResource";
        }

        public static class Internships
        {
            public const string NotFound = "Internships.NotFound";
            public const string InternshipIdRequired = "Internships.InternshipIdRequired";
        }

        public static class Logbooks
        {
            public const string NotFound = "Logbooks.NotFound";
            public const string InvalidInternship = "Logbooks.InvalidInternship";
            public const string CreationFailed = "Logbooks.CreationFailed";
            public const string UpdateFailed = "Logbooks.UpdateFailed";
            public const string DeleteFailed = "Logbooks.DeleteFailed";
            public const string AlreadyReported = "Logbooks.AlreadyReported";
            public const string StudentNotFound = "Logbooks.StudentNotFound";
            public const string UpdateForbidden = "Logbooks.UpdateForbidden";
            public const string DeleteForbidden = "Logbooks.DeleteForbidden";
        }

        public static class Page
        {
            public const string PageNumberMinValue = "PageNumber.MinValue";
            public const string PageSizeMinValue = "PageSize.MinValue";
            public const string PageSizeMaxValue = "PageSize.MaxValue";
            public const string SearchTermMaxLength = "SearchTerm.MaxLength";
            public const string SortColumnAllowedValues = "SortColumn.AllowedValues";
            public const string SortOrderAllowedValues = "SortOrder.AllowedValues";
        }

        public static class Sprint
        {
            public const string NameRequired = "Sprint.NameRequired";
            public const string NameMaxLength = "Sprint.NameMaxLength";
            public const string GoalMaxLength = "Sprint.GoalMaxLength";
            public const string StartDateRequired = "Sprint.StartDateRequired";
            public const string EndDateRequired = "Sprint.EndDateRequired";
            public const string EndDateMustBeAfterStart = "Sprint.EndDateMustBeAfterStart";
            public const string DurationTooShort = "Sprint.DurationTooShort";
            public const string DurationTooLong = "Sprint.DurationTooLong";
            public const string NotFound = "Sprint.NotFound";
            public const string AlreadyActive = "Sprint.AlreadyActive";
            public const string NotPlanned = "Sprint.NotPlanned";
            public const string NotActive = "Sprint.NotActive";
            public const string ActiveSprintExists = "Sprint.ActiveSprintExists";
            public const string CannotDeleteActive = "Sprint.CannotDeleteActive";
            public const string CannotDeleteActiveSprint = "Sprint.CannotDeleteActiveSprint";
            public const string CannotDeleteWithWorkItems = "Sprint.CannotDeleteWithWorkItems";
            public const string CannotEditCompleted = "Sprint.CannotEditCompleted";
            public const string DatesRequiredToStart = "Sprint.DatesRequiredToStart";
            public const string InvalidIncompleteItemsOption = "Sprint.InvalidIncompleteItemsOption";
            public const string TargetSprintNotFound = "Sprint.TargetSprintNotFound";
            public const string TargetSprintIdRequired = "Sprint.TargetSprintIdRequired";
            public const string NewSprintNameRequired = "Sprint.NewSprintNameRequired";
        }

        public static class Epic
        {
            public const string NameRequired = "Epic.NameRequired";
            public const string NameMaxLength = "Epic.NameMaxLength";
            public const string DescriptionMaxLength = "Epic.DescriptionMaxLength";
            public const string NotFound = "Epic.NotFound";
            public const string CannotDeleteWithChildren = "Epic.CannotDeleteWithChildren";
            public const string ProjectNotFound = "Epic.ProjectNotFound";
            public const string EpicIdRequired = "Epic.EpicIdRequired";
        }

        public static class WorkItem
        {
            public const string TitleRequired = "WorkItem.TitleRequired";
            public const string TitleMaxLength = "WorkItem.TitleMaxLength";
            public const string TypeRequired = "WorkItem.TypeRequired";
            public const string TypeInvalid = "WorkItem.TypeInvalid";
            public const string PriorityInvalid = "WorkItem.PriorityInvalid";
            public const string StatusInvalid = "WorkItem.StatusInvalid";
            public const string StoryPointInvalid = "WorkItem.StoryPointInvalid";
            public const string NotFound = "WorkItem.NotFound";
        }

        public static class Stakeholder
        {
            public const string NotFound = "Stakeholder.NotFound";
            public const string ProjectNotFound = "Stakeholder.ProjectNotFound";
            public const string EmailExists = "Stakeholder.EmailExists";
            public const string CreateSuccess = "Stakeholder.CreateSuccess";
            public const string UpdateSuccess = "Stakeholder.UpdateSuccess";
            public const string DeleteSuccess = "Stakeholder.DeleteSuccess";
            public const string IdRequired = "Stakeholder.IdRequired";
            public const string InternshipIdRequired = "Stakeholder.InternshipIdRequired";
            public const string ProjectIdRequired = "Stakeholder.ProjectIdRequired";
            public const string NameRequired = "Stakeholder.NameRequired";
            public const string NameMaxLength = "Stakeholder.NameMaxLength";
            public const string EmailRequired = "Stakeholder.EmailRequired";
            public const string EmailInvalid = "Stakeholder.EmailInvalid";
            public const string EmailMaxLength = "Stakeholder.EmailMaxLength";
            public const string RoleMaxLength = "Stakeholder.RoleMaxLength";
            public const string DescriptionMaxLength = "Stakeholder.DescriptionMaxLength";
            public const string PhoneNumberInvalid = "Stakeholder.PhoneNumberInvalid";
            public const string PhoneNumberMaxLength = "Stakeholder.PhoneNumberMaxLength";
            public const string InvalidType = "Stakeholder.InvalidType";
        }

        public static class Issue
        {
            public const string NotFound = "Issue.NotFound";
            public const string StakeholderNotFound = "Issue.StakeholderNotFound";
            public const string TitleRequired = "Issue.TitleRequired";
            public const string DescriptionRequired = "Issue.DescriptionRequired";
            public const string StakeholderIdRequired = "Issue.StakeholderIdRequired";
            public const string InvalidStatus = "Issue.InvalidStatus";
            public const string CreateSuccess = "Issue.CreateSuccess";
            public const string CreateFailed = "Issue.CreateFailed";
            public const string UpdateStatusSuccess = "Issue.UpdateStatusSuccess";
            public const string UpdateFailed = "Issue.UpdateFailed";
            public const string DeleteSuccess = "Issue.DeleteSuccess";
            public const string DeleteFailed = "Issue.DeleteFailed";
        }


        public static class Validation
        {
            public const string NameMaxLength = "Validation.NameMaxLength";
            public const string DescriptionMaxLength = "Validation.DescriptionMaxLength";
            public const string IdRequired = "Validation.IdRequired";
            public const string UserInvalidRole = "Validation.User.InvalidRole";
            public const string UserUnitRequired = "Validation.User.UnitRequired";
            public const string UserInvalidStatus = "Validation.User.InvalidStatus";
            public const string UserInvalidPhone = "Validation.User.InvalidPhone";
            public const string UserInvalidGender = "Validation.User.InvalidGender";
            public const string UserInvalidDateFormat = "Validation.User.InvalidDateFormat";
        }

        public static class Error
        {
            public const string WorkItemNotFound = "Error.WorkItem.NotFound";
        }

        public static class EvaluationCycle
        {
            public const string NotFound = "EvaluationCycle.NotFound";
            public const string NameRequired = "EvaluationCycle.NameRequired";
            public const string NameMaxLength = "EvaluationCycle.NameMaxLength";
            public const string TermNotFound = "EvaluationCycle.TermNotFound";
            public const string StartDateRequired = "EvaluationCycle.StartDateRequired";
            public const string EndDateRequired = "EvaluationCycle.EndDateRequired";
            public const string EndDateMustBeAfterStart = "EvaluationCycle.EndDateMustBeAfterStart";
            public const string CannotDeleteWithCriteria = "EvaluationCycle.CannotDeleteWithCriteria";
            public const string CannotUpdateCompleted = "EvaluationCycle.CannotUpdateCompleted";
            public const string AlreadyCompleted = "EvaluationCycle.AlreadyCompleted";
            public const string CannotCompleteWithoutCriteria = "EvaluationCycle.CannotCompleteWithoutCriteria";
        }

        public static class EvaluationCriteriaKey
        {
            public const string NotFound = "EvaluationCriteria.NotFound";
            public const string CycleNotFound = "EvaluationCriteria.CycleNotFound";
            public const string NameRequired = "EvaluationCriteria.NameRequired";
            public const string NameMaxLength = "EvaluationCriteria.NameMaxLength";
            public const string MaxScoreInvalid = "EvaluationCriteria.MaxScoreInvalid";
            public const string WeightInvalid = "EvaluationCriteria.WeightInvalid";
            public const string CannotCreateInCompletedCycle = "EvaluationCriteria.CannotCreateInCompletedCycle";
            public const string CannotUpdateInCompletedCycle = "EvaluationCriteria.CannotUpdateInCompletedCycle";
            public const string CannotDeleteInCompletedCycle = "EvaluationCriteria.CannotDeleteInCompletedCycle";
        }

        public static class EvaluationKey
        {
            public const string NotFound = "Evaluation.NotFound";
            public const string CycleNotFound = "Evaluation.CycleNotFound";
            public const string StudentNotFound = "Evaluation.StudentNotFound";
            public const string InternshipNotFound = "Evaluation.InternshipNotFound";
            public const string StudentNotInGroup = "Evaluation.StudentNotInGroup";
            public const string AlreadyExists = "Evaluation.AlreadyExists";
            public const string CannotUpdateSubmitted = "Evaluation.CannotUpdateSubmitted";
            public const string CriteriaNotFound = "Evaluation.CriteriaNotFound";
            public const string ScoreExceedsMax = "Evaluation.ScoreExceedsMax";
            public const string AlreadySubmitted = "Evaluation.AlreadySubmitted";
            public const string CannotPublishIfNotSubmitted = "Evaluation.CannotPublishIfNotSubmitted";
            public const string CannotSaveInCompletedCycle = "Evaluation.CannotSaveInCompletedCycle";
            public const string CannotUpdateInCompletedCycle = "Evaluation.CannotUpdateInCompletedCycle";
            public const string CannotSubmitInCompletedCycle = "Evaluation.CannotSubmitInCompletedCycle";
            public const string CannotPublishInCompletedCycle = "Evaluation.CannotPublishInCompletedCycle";
        }

        public static class InternshipApplication
        {
            // Validation
            public const string ApplicationIdRequired = "InternshipApplication.ApplicationIdRequired";
            public const string MentorIdRequired = "InternshipApplication.MentorIdRequired";
            public const string RejectReasonRequired = "InternshipApplication.RejectReasonRequired";
            public const string RejectReasonMaxLength = "InternshipApplication.RejectReasonMaxLength";
            public const string ProjectNameRequired = "InternshipApplication.ProjectNameRequired";
            public const string ProjectNameMaxLength = "InternshipApplication.ProjectNameMaxLength";
            public const string ProjectDescriptionMaxLength = "InternshipApplication.ProjectDescriptionMaxLength";

            // Business errors
            public const string NotFound = "InternshipApplication.NotFound";
            public const string EnterpriseUserNotFound = "InternshipApplication.EnterpriseUserNotFound";
            public const string MentorNotBelongToEnterprise = "InternshipApplication.MentorNotBelongToEnterprise";
            public const string StatusMustBePendingToAccept = "InternshipApplication.StatusMustBePendingToAccept";
            public const string StatusMustBePendingToReject = "InternshipApplication.StatusMustBePendingToReject";
            public const string StatusMustBeApprovedToAssign = "InternshipApplication.StatusMustBeApprovedToAssign";
            public const string StudentAlreadyInGroup = "InternshipApplication.StudentAlreadyInGroup";
            public const string StudentNotInMentorGroup = "InternshipApplication.StudentNotInMentorGroup";
            public const string ProjectNameExistsInGroup = "InternshipApplication.ProjectNameExistsInGroup";

            // Success messages
            public const string AcceptSuccess = "InternshipApplication.AcceptSuccess";
            public const string RejectSuccess = "InternshipApplication.RejectSuccess";
            public const string AssignMentorSuccess = "InternshipApplication.AssignMentorSuccess";
            public const string AssignMentorNewGroupSuccess = "InternshipApplication.AssignMentorNewGroupSuccess";
            public const string AssignMentorExistingGroupSuccess = "InternshipApplication.AssignMentorExistingGroupSuccess";
            public const string AssignProjectSuccess = "InternshipApplication.AssignProjectSuccess";

            // Log keys
            public const string LogAccepting = "InternshipApplication.Log.Accepting";
            public const string LogAcceptSuccess = "InternshipApplication.Log.AcceptSuccess";
            public const string LogAcceptError = "InternshipApplication.Log.AcceptError";
            public const string LogRejecting = "InternshipApplication.Log.Rejecting";
            public const string LogRejectSuccess = "InternshipApplication.Log.RejectSuccess";
            public const string LogRejectError = "InternshipApplication.Log.RejectError";
            public const string LogAssigningMentor = "InternshipApplication.Log.AssigningMentor";
            public const string LogAssignMentorSuccess = "InternshipApplication.Log.AssignMentorSuccess";
            public const string LogAssignMentorError = "InternshipApplication.Log.AssignMentorError";
            public const string LogAssigningProject = "InternshipApplication.Log.AssigningProject";
            public const string LogAssignProjectSuccess = "InternshipApplication.Log.AssignProjectSuccess";
            public const string LogAssignProjectError = "InternshipApplication.Log.AssignProjectError";
            public const string LogInvalidUserId = "InternshipApplication.Log.InvalidUserId";
        }
        public static class File
        {

        }

        public static class StudentTerms
        {
            // Domain errors
            public const string NotFound = "StudentTerms.NotFound";
            public const string TermNotOpen = "StudentTerms.TermNotOpen";
            public const string TermEndedOrClosed = "StudentTerms.TermEndedOrClosed";
            public const string CannotWithdrawPlacedViaUpdate = "StudentTerms.CannotWithdrawPlacedViaUpdate";
            public const string EmailConflict = "StudentTerms.EmailConflict";
            public const string StudentCodeConflict = "StudentTerms.StudentCodeConflict";
            public const string AlreadyEnrolled = "StudentTerms.AlreadyEnrolled";
            public const string AlreadyWithdrawn = "StudentTerms.AlreadyWithdrawn";
            public const string NotWithdrawn = "StudentTerms.NotWithdrawn";
            public const string CannotWithdrawPlaced = "StudentTerms.CannotWithdrawPlaced";
            public const string EnterpriseIdRequiredWhenPlaced = "StudentTerms.EnterpriseIdRequiredWhenPlaced";
            public const string EnterpriseNotFound = "StudentTerms.EnterpriseNotFound";
            public const string AllStudentsPlaced = "StudentTerms.AllStudentsPlaced";
            public const string InvalidFileFormat = "StudentTerms.InvalidFileFormat";
            public const string FileTooLarge = "StudentTerms.FileTooLarge";
            public const string InvalidExcelHeaders = "StudentTerms.InvalidExcelHeaders";
            public const string InvalidExcelHeaderDetail = "StudentTerms.InvalidExcelHeaderDetail";
            public const string InvalidExcelHeaderEmpty = "StudentTerms.InvalidExcelHeaderEmpty";
            public const string TooManyRows = "StudentTerms.TooManyRows";
            public const string CannotDeleteFromSystemHasOtherTerms = "StudentTerms.CannotDeleteFromSystemHasOtherTerms";

            // Validation — TermId / StudentTermId
            public const string TermIdRequired = "StudentTerms.TermIdRequired";
            public const string StudentTermIdRequired = "StudentTerms.StudentTermIdRequired";
            public const string StudentTermIdListRequired = "StudentTerms.StudentTermIdListRequired";
            public const string StudentTermIdListMinCount = "StudentTerms.StudentTermIdListMinCount";
            public const string ValidRecordsRequired = "StudentTerms.ValidRecordsRequired";
            public const string ValidRecordsMinCount = "StudentTerms.ValidRecordsMinCount";

            // Validation — File
            public const string FileRequired = "StudentTerms.FileRequired";
            public const string FileEmpty = "StudentTerms.FileEmpty";

            // Validation — FullName
            public const string FullNameRequired = "StudentTerms.FullNameRequired";
            public const string FullNameInvalid = "StudentTerms.FullNameInvalid";

            // Validation — StudentCode
            public const string StudentCodeRequired = "StudentTerms.StudentCodeRequired";
            public const string StudentCodeInvalid = "StudentTerms.StudentCodeInvalid";
            public const string StudentCodeInvalidDetail = "StudentTerms.StudentCodeInvalidDetail";
            public const string StudentCodeDuplicateInFile = "StudentTerms.StudentCodeDuplicateInFile";
            public const string StudentCodeAlreadyInTerm = "StudentTerms.StudentCodeAlreadyInTerm";
            public const string StudentCodeInOtherTerm = "StudentTerms.StudentCodeInOtherTerm";

            // Validation — Email
            public const string EmailRequired = "StudentTerms.EmailRequired";
            public const string EmailInvalid = "StudentTerms.EmailInvalid";
            public const string EmailDuplicateInFile = "StudentTerms.EmailDuplicateInFile";
            public const string EmailAlreadyInTerm = "StudentTerms.EmailAlreadyInTerm";
            public const string EmailInOtherTerm = "StudentTerms.EmailInOtherTerm";

            // Validation — Phone
            public const string PhoneInvalid = "StudentTerms.PhoneInvalid";

            // Validation — DateOfBirth
            public const string DateOfBirthInvalidFormat = "StudentTerms.DateOfBirthInvalidFormat";
            public const string DateOfBirthMinAge = "StudentTerms.DateOfBirthMinAge";

            // Validation — List query
            public const string SearchTermMaxLength = "StudentTerms.SearchTermMaxLength";
            public const string SortByAllowedValues = "StudentTerms.SortByAllowedValues";
            public const string SortOrderAllowedValues = "StudentTerms.SortOrderAllowedValues";

            // Success messages
            public const string AddSuccess = "StudentTerms.AddSuccess";
            public const string UpdateSuccess = "StudentTerms.UpdateSuccess";
            public const string WithdrawSuccess = "StudentTerms.WithdrawSuccess";
            public const string WithdrawDeleteWarning = "StudentTerms.WithdrawDeleteWarning";
            public const string BulkWithdrawSuccess = "StudentTerms.BulkWithdrawSuccess";
            public const string ImportPreviewSuccess = "StudentTerms.ImportPreviewSuccess";
            public const string ImportConfirmSuccess = "StudentTerms.ImportConfirmSuccess";
            public const string GetStudentsSuccess = "StudentTerms.GetStudentsSuccess";
            public const string GetStudentTermDetailSuccess = "StudentTerms.GetStudentTermDetailSuccess";
            public const string DownloadTemplateSuccess = "StudentTerms.DownloadTemplateSuccess";

            // Email notifications
            public const string EmailSubjectWithdraw = "StudentTerms.Email.SubjectWithdraw";
            public const string EmailBodyWithdraw = "StudentTerms.Email.BodyWithdraw";

            // Excel headers
            public const string ExcelHeaderStudentCode = "StudentTerms.Excel.HeaderStudentCode";
            public const string ExcelHeaderFullName = "StudentTerms.Excel.HeaderFullName";
            public const string ExcelHeaderEmail = "StudentTerms.Excel.HeaderEmail";
            public const string ExcelHeaderPhone = "StudentTerms.Excel.HeaderPhone";
            public const string ExcelHeaderDateOfBirth = "StudentTerms.Excel.HeaderDateOfBirth";
            public const string ExcelHeaderMajor = "StudentTerms.Excel.HeaderMajor";
            public const string ExcelHeaderTempPassword = "StudentTerms.Excel.HeaderTempPassword";
            public const string ExcelWorksheetStudentList = "StudentTerms.Excel.WorksheetStudentList";
            public const string ExcelWorksheetTempPassword = "StudentTerms.Excel.WorksheetTempPassword";

            // Logging
            public const string LogAdded = "StudentTerms.Log.Added";
            public const string LogUpdated = "StudentTerms.Log.Updated";
            public const string LogWithdrawn = "StudentTerms.Log.Withdrawn";
            public const string LogBulkWithdrawn = "StudentTerms.Log.BulkWithdrawn";
            public const string LogImportConfirmed = "StudentTerms.Log.ImportConfirmed";
            public const string LogAddManualError = "StudentTerms.Log.AddManualError";
            public const string LogImportConfirmError = "StudentTerms.Log.ImportConfirmError";
        }
        public static class ActiveTerms
        {
            public const string InvalidUserId = "ActiveTerms.InvalidUserId";
            public const string EnterpriseUserNotFound = "ActiveTerms.EnterpriseUserNotFound";
            public const string NoActiveTermsFoundForEnterprise = "ActiveTerms.NoActiveTermsFoundForEnterprise";
            public const string NoActiveTermsFoundForMentor = "ActiveTerms.NoActiveTermsFoundForMentor";
            public const string SystemError = "ActiveTerms.SystemError";
            public const string LogRetrieved = "ActiveTerms.Log.Retrieved";
            public const string LogError = "ActiveTerms.Log.Error";
        }

        public static class ViolationReportKey
        {
            // Validator
            public const string OccurredDateIsRequired = "ViolationReport.OccurredDateIsRequired";
            public const string OccurredDateInFuture = "ViolationReport.OccurredDateInFuture";
            public const string DescriptionIsRequired = "ViolationReport.DescriptionIsRequired";
            public const string DescriptionMinLength = "ViolationReport.DescriptionMinLength";
            public const string DescriptionMaxLength = "ViolationReport.DescriptionMaxLength";
            // Get All
            public const string GetViolationReportsError = "ViolationReport.GetViolationReportsError";
            public const string FilteredNotFound = "ViolationReport.FilteredNotFound";
            public const string NotFound = "ViolationReport.NotFound";
            // Create
            public const string NotAllowedToReport = "ViolationReport.NotAllowedToReport";
            public const string OccurredDateCannotBeBeforeInternshipStart = "ViolationReport.OccurredDateCannotBeBeforeInternshipStart";
            public const string OccurredDateCannotBeAfterInternshipEnd = "ViolationReport.OccurredDateCannotBeAfterInternshipEnd";
            public const string CreateViolationReportSuccess = "ViolationReport.CreateViolationReportSuccess";
            public const string CreateViolationReportError = "ViolationReport.CreateViolationReportError";
            public const string ViolationReportLogNotFound = "ViolationReport.LogNotFound";
            // Delete
            public const string LogDeleteNotOwner = "ViolationReport.LogDeleteNotOwner";
            public const string DeleteNotOwner = "ViolationReport.DeleteNotOwner";
            public const string ViolationReportDeletedSuccessfully = "ViolationReport.ViolationReportDeletedSuccessfully";
            public const string ErrorOccurredWhileDeletingViolationReport = "ViolationReport.ErrorOccurredWhileDeletingViolationReport";
            // Update
            public const string UpdatingViolationReportByUser = "ViolationReport.UpdatingViolationReportByUser";
            public const string ViolationReportNotFound = "ViolationReport.ViolationReportNotFound";
            public const string UserNotAllowedToUpdateViolationReport = "ViolationReport.UserNotAllowedToUpdateViolationReport";
            public const string NotAllowedToUpdateThisReport = "ViolationReport.NotAllowedToUpdateThisReport";
            public const string OccurredDateBeforeInternshipStart = "ViolationReport.OccurredDateBeforeInternshipStart";
            public const string InternshipHasEnded = "ViolationReport.InternshipHasEnded";
            public const string ViolationReportUpdatedByAnotherUser = "ViolationReport.ViolationReportUpdatedByAnotherUser";
            public const string ViolationReportUpdatedSuccessfully = "ViolationReport.ViolationReportUpdatedSuccessfully";
            public const string FailedToUpdateViolationReport = "ViolationReport.FailedToUpdateViolationReport";
            public const string ErrorOccurredWhileUpdatingViolationReport = "ViolationReport.ErrorOccurredWhileUpdatingViolationReport";
            public const string ViolationReportIdIsRequired = "ViolationReport.ViolationReportIdIsRequired";
            public const string ErrorWhileGettingViolationReportDetail = "ViolationReport.ErrorWhileGettingViolationReportDetail";
        }

        public static class StudentMessageKey
        {
            //validator
            public const string StudentIdRequired = "Student.StudentIdRequired";
            //create
            public const string StudentNotFound = "Student.NotFound";
        }

        public static class JobPostingMessageKey
        {
            // Common
            public const string TitleRequired = "JobPosting.TitleRequired";
            public const string TitleTooLong = "JobPosting.TitleTooLong";
            public const string DescriptionTooLong = "JobPosting.DescriptionTooLong";
            public const string RequirementsTooLong = "JobPosting.RequirementsTooLong";
            public const string BenefitTooLong = "JobPosting.BenefitTooLong";
            public const string LocationRequired = "JobPosting.LocationRequired";
            public const string LocationTooLong = "JobPosting.LocationTooLong";
            public const string QuantityMustBePositive = "JobPosting.QuantityMustBePositive";
            public const string ExpireDateMustBeTodayOrLater = "JobPosting.ExpireDateMustBeTodayOrLater";
            public const string ExpireDateRequired = "JobPosting.ExpireDateRequired";
            public const string StartDateMustBeTodayOrLater = "JobPosting.StartDateMustBeTodayOrLater";
            public const string StartDateRequired = "JobPosting.StartDateRequired";
            public const string EndDateRequired = "JobPosting.EndDateRequired";
            public const string EndDateMinDuration = "JobPosting.EndDateMinDuration";
            public const string EndDateMaxDuration = "JobPosting.EndDateMaxDuration";
            public const string AudienceInvalid = "JobPosting.AudienceInvalid";
            public const string AudienceRequired = "JobPosting.AudienceRequired";
            public const string UniversityRequiredForTargetAudience = "JobPosting.UniversityRequiredForTargetAudience";
            public const string NotAllowed = "JobPosting.NotAllowed";
            public const string InvalidStatus = "JobPosting.InvalidStatus";
            //Apply job
            public const string CvRequired = "JobPosting.CvRequired";
            public const string AlreadyPlaced = "JobPosting.AlreadyPlaced";
            //Close job
            public const string OnlyPublishedAllowed = "JobPosting.OnlyPublishedAllowed";
            public const string ConfirmHasActiveApplications = "JobPosting.ConfirmHasActiveApplications";
            public const string NotifyStudentClosedSubject = "JobPosting.NotifyStudentClosedSubject";
            public const string NotifyStudentClosedBody = "JobPosting.NotifyStudentClosedBody";
            public const string CloseSuccess = "JobPosting.CloseSuccess";
            // CreateDraft
            public const string DraftSavedSuccess = "JobPosting.DraftSavedSuccess";
            // Create Job
            public const string CreateSuccess = "JobPosting.CreateSuccess";
            public const string InternshipPhaseStatusAllowed = "JobPosting.InternshipPhaseStatusAllowed";
            // Delete
            public const string AlreadyDeleted = "JobPosting.AlreadyDeleted";
            public const string DeleteConfirmHasActiveApplications = "JobPosting.DeleteConfirmHasActiveApplications";
            public const string DeleteSuccess = "JobPosting.DeleteSuccess";
            public const string DeleteWithActiveApplications = "JobPosting.DeleteWithActiveApplications";
            public const string DleteVersionConflict = "JobPosting.DeleteVersionConflict";
            // Publish Job
            public const string JobPostingNotFound = "JobPosting.NotFound";
            public const string PublishSuccess = "JobPosting.PublishSuccess";
            public const string AlreadyPublished = "JobPosting.AlreadyPublished";
            // Update Job
            public const string UpdateConfirmHasApplications = "JobPosting.UpdateConfirmHasApplications";
            public const string ReopenExpireDateInvalid = "JobPosting.ReopenExpireDateInvalid";
            public const string UpdateQuantityLessThanPlaced = "JobPosting.UpdateQuantityLessThanPlaced";
            public const string UpdateInvalidUniversities = "JobPosting.UpdateInvalidUniversities";
            public const string TargetedRequiresSingleUniversity = "JobPosting.TargetedRequiresSingleUniversity";
            public const string ReopenNotifyStudentSubject = "JobPosting.ReopenNotifyStudentSubject";
            public const string ReopenNotifyStudentBody = "JobPosting.ReopenNotifyStudentBody";
            public const string ReopenSuccess = "JobPosting.ReopenSuccess";
            public const string UpdateNotifyStudentSubject = "JobPosting.UpdateNotifyStudentSubject";
            public const string UpdateNotifyStudentBody = "JobPosting.UpdateNotifyStudentBody";
            public const string ChangeInternPhaseBlockedDueToActiveApplications = "JobPosting.ChangeInternPhaseBlockedDueToActiveApplications";
            public const string QuantityCannotBeLessThanPlaced = "JobPosting.QuantityCannotBeLessThanPlaced";
            public const string DraftNoApplications = "JobPosting.DraftNoApplications";
            // Apply Job
            public const string UploadCVRequired = "JobPosting.UploadCVRequired";
            public const string CannotApplyWhenPlaced = "JobPosting.CannotApplyWhenPlaced";
            public const string PositionNotOpenForApplication = "JobPosting.PositionNotOpenForApplication";
            public const string ApplicationDeadlinePassed = "JobPosting.ApplicationDeadlinePassed";
            public const string NoActiveInternshipPeriod = "JobPosting.NoActiveInternshipPeriod";
            public const string AlreadyHaveActiveApplication = "JobPosting.AlreadyHaveActiveApplication";
            public const string ApplicationLimitReached = "JobPosting.ApplicationLimitReached";
            public const string StudentAppliedToEnterpriseJob = "JobPosting.StudentAppliedToEnterpriseJob";
            public const string ApplySuccessPendingHR = "JobPosting.ApplySuccessPendingHR";
            // Get Jobs
            public const string InternshipInProgress = "JobPosting.InternshipInProgress";
            public const string JobPlacedMaxed = "JobPosting.JobPlacedMaxed";
            // Reopen Job
            public const string InvalidStatusForReopen = "JobPosting.InvalidStatusForReopen";
            public const string ExpireDateMustBeFuture = "JobPosting.ExpireDateMustBeFuture";
            public const string ExpireDateExceedsPhaseEndDate = "JobPosting.ExpireDateExceedsPhaseEndDate";
            public const string ReopenJobPostingSuccessNotificationMessage = "JobPosting.ReopenJobPostingSuccessNotificationMessage";
        }
        public static class InternshipPhase
        {
            // Validator
            public const string EnterpriseIdRequired = "InternshipPhase.EnterpriseIdRequired";
            public const string PhaseIdRequired = "InternshipPhase.PhaseIdRequired";
            public const string NameRequired = "InternshipPhase.NameRequired";
            public const string NameMaxLength = "InternshipPhase.NameMaxLength";
            public const string StartDateRequired = "InternshipPhase.StartDateRequired";
            public const string StartDateNotInPast = "InternshipPhase.StartDateNotInPast";
            public const string EndDateRequired = "InternshipPhase.EndDateRequired";
            public const string EndDateAfterStartDate = "InternshipPhase.EndDateAfterStartDate";
            public const string DurationMinDays = "InternshipPhase.DurationMinDays";
            public const string DurationMaxDays = "InternshipPhase.DurationMaxDays";
            public const string MajorFieldsRequired = "InternshipPhase.MajorFieldsRequired";
            public const string MajorFieldsMaxLength = "InternshipPhase.MajorFieldsMaxLength";
            public const string MaxStudentsGreaterThanZero = "InternshipPhase.MaxStudentsGreaterThanZero";
            public const string StatusInvalid = "InternshipPhase.StatusInvalid";
            public const string DescriptionMaxLength = "InternshipPhase.DescriptionMaxLength";
            public const string PageNumberMinValue = "InternshipPhase.PageNumberMinValue";
            public const string PageSizeRange = "InternshipPhase.PageSizeRange";
            public const string InternshipPhaseIdRequired = "InternshipPhase.InternshipPhaseIdRequired";

            // Business errors
            public const string NotFound = "InternshipPhase.NotFound";
            public const string DuplicateName = "InternshipPhase.DuplicateName";
            public const string DuplicateNameOnUpdate = "InternshipPhase.DuplicateNameOnUpdate";
            public const string CannotUpdateClosed = "InternshipPhase.CannotUpdateClosed";
            public const string CannotUpdateEnded = "InternshipPhase.CannotUpdateEnded";
            public const string CannotUpdateLockedFields = "InternshipPhase.CannotUpdateLockedFields";
            public const string InvalidStatusTransition = "InternshipPhase.InvalidStatusTransition";
            public const string CannotDeleteHasActiveGroups = "InternshipPhase.CannotDeleteHasActiveGroups";
            public const string CannotDeleteInProgress = "InternshipPhase.CannotDeleteInProgress";
            public const string UpdateNoChanges = "InternshipPhase.UpdateNoChanges";
            public const string StudentNotFound = "InternshipPhase.StudentNotFound";
            public const string NotYourEnterprise = "InternshipPhase.NotYourEnterprise";
            public const string EnterpriseUserNotFound = "InternshipPhase.EnterpriseUserNotFound";
            public const string EndDateInPastForActivePhase = "InternshipPhase.EndDateInPastForActivePhase";

            // Log - Ownership
            public const string LogOwnershipDenied = "InternshipPhase.Log.OwnershipDenied";
            public const string LogInvalidStatusTransition = "InternshipPhase.Log.InvalidStatusTransition";

            // Success
            public const string CreateSuccess = "InternshipPhase.CreateSuccess";
            public const string UpdateSuccess = "InternshipPhase.UpdateSuccess";
            public const string DeleteSuccess = "InternshipPhase.DeleteSuccess";

            // Log - Create
            public const string LogCreating = "InternshipPhase.Log.Creating";
            public const string LogEnterpriseNotFound = "InternshipPhase.Log.EnterpriseNotFound";
            public const string LogDuplicateName = "InternshipPhase.Log.DuplicateName";
            public const string LogCreateSuccess = "InternshipPhase.Log.CreateSuccess";
            public const string LogCreateError = "InternshipPhase.Log.CreateError";

            // Log - Update
            public const string LogUpdating = "InternshipPhase.Log.Updating";
            public const string LogUpdateNotFound = "InternshipPhase.Log.UpdateNotFound";
            public const string LogUpdateClosed = "InternshipPhase.Log.UpdateClosed";
            public const string LogUpdateDuplicateName = "InternshipPhase.Log.UpdateDuplicateName";
            public const string LogUpdateSuccess = "InternshipPhase.Log.UpdateSuccess";
            public const string LogUpdateNoChanges = "InternshipPhase.Log.UpdateNoChanges";
            public const string LogUpdateError = "InternshipPhase.Log.UpdateError";

            // Log - Delete
            public const string LogDeleting = "InternshipPhase.Log.Deleting";
            public const string LogDeleteNotFound = "InternshipPhase.Log.DeleteNotFound";
            public const string LogDeleteHasActiveGroups = "InternshipPhase.Log.DeleteHasActiveGroups";
            public const string LogDeleteInProgress = "InternshipPhase.Log.DeleteInProgress";
            public const string LogDeleteSuccess = "InternshipPhase.Log.DeleteSuccess";
            public const string LogDeleteNoChanges = "InternshipPhase.Log.DeleteNoChanges";
            public const string LogDeleteError = "InternshipPhase.Log.DeleteError";

            // Log - Get List
            public const string LogGettingList = "InternshipPhase.Log.GettingList";
            public const string LogListFromCache = "InternshipPhase.Log.ListFromCache";
            public const string LogListSuccess = "InternshipPhase.Log.ListSuccess";

            // Log - Get By Id
            public const string LogGettingById = "InternshipPhase.Log.GettingById";
            public const string LogByIdFromCache = "InternshipPhase.Log.ByIdFromCache";
            public const string LogByIdNotFound = "InternshipPhase.Log.ByIdNotFound";
            public const string LogByIdSuccess = "InternshipPhase.Log.ByIdSuccess";
            /// <summary>AC-05: log how many job postings and placed students were loaded.</summary>
            public const string LogByIdTabsLoaded = "InternshipPhase.Log.ByIdTabsLoaded";


            // Log - Get My Phases (Student + Mentor)
            public const string LogGettingMyPhases           = "InternshipPhase.Log.GettingMyPhases";
            public const string LogStudentNotFound           = "InternshipPhase.Log.StudentNotFound";
            public const string LogMyPhasesSuccess           = "InternshipPhase.Log.MyPhasesSuccess";
            public const string LogMentorUserNotFound        = "InternshipPhase.Log.MentorUserNotFound";
            public const string LogMyMentorPhasesSuccess     = "InternshipPhase.Log.MyMentorPhasesSuccess";
            public const string MentorEnterpriseUserNotFound = "InternshipPhase.MentorEnterpriseUserNotFound";
        }

        public static class Notifications
        {
            public const string NotFound = "Notifications.NotFound";
            public const string AccessDenied = "Notifications.AccessDenied";
            public const string MarkReadSuccess = "Notifications.MarkReadSuccess";
            public const string MarkAllReadSuccess = "Notifications.MarkAllReadSuccess";
            public const string DeleteSuccess = "Notifications.DeleteSuccess";
            public const string DeleteError = "Notifications.DeleteError";
            public const string BulkDeleteSuccess = "Notifications.BulkDeleteSuccess";
            public const string DeleteNotOwner = "Notifications.DeleteNotOwner";
            public const string BulkDeleteEmptyIds = "Notifications.BulkDeleteEmptyIds";
            public const string NotificationIdRequired = "Notifications.NotificationIdRequired";
        }
        public static class HRApplications
        {
            // Validation
            public const string ApplicationIdRequired = "HRApplications.ApplicationIdRequired";
            public const string RejectReasonRequired = "HRApplications.RejectReasonRequired";
            public const string RejectReasonMaxLength = "HRApplications.RejectReasonMaxLength";
            public const string RejectReasonMinLength = "HRApplications.RejectReasonMinLength";

            public const string NotFound = "HRApplications.NotFound";
            public const string EnterpriseUserNotFound = "HRApplications.EnterpriseUserNotFound";
            public const string InvalidTransition = "HRApplications.InvalidTransition";
            public const string CannotRejectPlaced = "HRApplications.CannotRejectPlaced";
            public const string NotSelfApplyApplication = "HRApplications.NotSelfApplyApplication";
            public const string NotUniAssignApplication = "HRApplications.NotUniAssignApplication";
            public const string ApplicationNotActive = "HRApplications.ApplicationNotActive";
            public const string ApplicationNotPlaced = "HRApplications.ApplicationNotPlaced";
            public const string InternPhaseAtCapacity = "HRApplications.InternPhaseAtCapacity";

            // Notification message templates
            public const string NotifyInterviewing = "HRApplications.Notify.Interviewing";
            public const string NotifyOffered = "HRApplications.Notify.Offered";
            public const string NotifyPlacedSelfApply = "HRApplications.Notify.Placed.SelfApply";
            public const string NotifyPlacedUniAssign = "HRApplications.Notify.Placed.UniAssign";
            public const string NotifyRejectedSelfApply = "HRApplications.Notify.Rejected.SelfApply";
            public const string NotifyRejectedUniAssign = "HRApplications.Notify.Rejected.UniAssign";
            public const string NotifyUniAdminRejected = "HRApplications.Notify.UniAdmin.Rejected";
            public const string NotifyUniAdminPlaced = "HRApplications.Notify.UniAdmin.Placed";
            public const string NotifyEnterpriseAutoWithdrawn = "HRApplications.Notify.Enterprise.AutoWithdrawn";
            public const string NotifyStudentAutoWithdrawn = "HRApplications.Notify.Student.AutoWithdrawn";

            // Logging
            public const string LogMoveToInterviewing = "HRApplications.Log.MoveToInterviewing";
            public const string LogSendOffer = "HRApplications.Log.SendOffer";
            public const string LogMarkAsPlaced = "HRApplications.Log.MarkAsPlaced";
            public const string LogReject = "HRApplications.Log.Reject";
            public const string LogApproveUniAssign = "HRApplications.Log.ApproveUniAssign";
            public const string LogRejectUniAssign = "HRApplications.Log.RejectUniAssign";
            public const string LogRemovePlacedUniAssign = "HRApplications.Log.RemovePlacedUniAssign";
            public const string LogCascadeWithdraw = "HRApplications.Log.CascadeWithdraw";
            public const string LogInvalidUserId = "HRApplications.Log.InvalidUserId";
        }

        public static class StudentApplications
        {
            // Business errors
            public const string NotFound = "StudentApplications.NotFound";
            public const string NotOwner = "StudentApplications.NotOwner";
            public const string CannotWithdrawNotApplied = "StudentApplications.CannotWithdrawNotApplied";
            public const string CannotHidePlaced = "StudentApplications.CannotHidePlaced";
            public const string CannotHideActiveApplication = "StudentApplications.CannotHideActiveApplication";

            // Success
            public const string WithdrawSuccess = "StudentApplications.WithdrawSuccess";
            public const string HideSuccess = "StudentApplications.HideSuccess";

            // Notify HR (AC-07)
            public const string NotifyHRWithdrawn = "StudentApplications.Notify.HR.Withdrawn";

            // Notify SV + Uni Admin when HR removes a Placed student (AC-C05)
            public const string NotifyStudentRemovedPlaced = "StudentApplications.Notify.Student.RemovedPlaced";
            public const string NotifyUniAdminRemovedPlaced = "StudentApplications.Notify.UniAdmin.RemovedPlaced";
        }

        public static class UniAdminInternship
        {
            // Errors
            public const string UniversityUserNotFound = "UniAdminInternship.UniversityUserNotFound";
            public const string TermNotFound = "UniAdminInternship.TermNotFound";
            public const string TermAccessDenied = "UniAdminInternship.TermAccessDenied";
            public const string StudentNotFound = "UniAdminInternship.StudentNotFound";
            public const string StudentNotInUniversity = "UniAdminInternship.StudentNotInUniversity";
            public const string NoOpenTermFound = "UniAdminInternship.NoOpenTermFound";

            // Success
            public const string StudentsRetrieved = "UniAdminInternship.StudentsRetrieved";
            public const string StudentDetailRetrieved = "UniAdminInternship.StudentDetailRetrieved";
            public const string StudentLogbookTotalRetrieved = "UniAdminInternship.StudentLogbookTotalRetrieved";
            public const string StudentLogbookWeeklyRetrieved = "UniAdminInternship.StudentLogbookWeeklyRetrieved";
            public const string EvaluationsRetrieved = "UniAdminInternship.EvaluationsRetrieved";
            public const string ViolationsRetrieved = "UniAdminInternship.ViolationsRetrieved";

            // Log
            public const string LogGetStudentList = "UniAdminInternship.Log.GetStudentList";
            public const string LogGetStudentDetail = "UniAdminInternship.Log.GetStudentDetail";
            public const string LogGetLogbookTotal = "UniAdminInternship.Log.GetLogbookTotal";
            public const string LogGetLogbookWeekly = "UniAdminInternship.Log.GetLogbookWeekly";
            public const string LogGetEvaluations = "UniAdminInternship.Log.GetEvaluations";
            public const string LogGetViolations = "UniAdminInternship.Log.GetViolations";
            public const string LogUniversityUserNotFound = "UniAdminInternship.Log.UniversityUserNotFound";
            public const string LogTermNotFound = "UniAdminInternship.Log.TermNotFound";
            public const string LogTermAccessDenied = "UniAdminInternship.Log.TermAccessDenied";
            public const string LogStudentNotFound = "UniAdminInternship.Log.StudentNotFound";
            public const string LogNoOpenTerm = "UniAdminInternship.Log.NoOpenTerm";

            // Validation
            public const string PageNumberInvalid = "UniAdminInternship.PageNumberInvalid";
            public const string PageSizeInvalid = "UniAdminInternship.PageSizeInvalid";
            public const string PageSizeTooLarge = "UniAdminInternship.PageSizeTooLarge";
            public const string StudentIdRequired = "UniAdminInternship.StudentIdRequired";

            // Logbook weekly display
            public const string StatusBadgeLate = "UniAdminInternship.StatusBadge.Late";
            public const string StatusBadgeSubmitted = "UniAdminInternship.StatusBadge.Submitted";
            public const string StatusBadgeWeekend = "UniAdminInternship.StatusBadge.Weekend";
            public const string StatusBadgeHoliday = "UniAdminInternship.StatusBadge.Holiday";
            public const string StatusBadgePending = "UniAdminInternship.StatusBadge.Pending";
            public const string StatusBadgeMissing = "UniAdminInternship.StatusBadge.Missing";
            public const string WeekTitle = "UniAdminInternship.WeekTitle";
        }
    }
}

