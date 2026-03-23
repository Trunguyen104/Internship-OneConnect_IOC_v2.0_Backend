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

            public const string LogAddingStudents = "InternshipGroup.LogAddingStudents";
            public const string LogInvalidStudentIds = "InternshipGroup.LogInvalidStudentIds";
            public const string LogAddedStudentsSuccess = "InternshipGroup.LogAddedStudentsSuccess";
            public const string LogAddStudentsFailed = "InternshipGroup.LogAddStudentsFailed";
            public const string LogAddStudentsError = "InternshipGroup.LogAddStudentsError";

            public const string LogRemovingStudents = "InternshipGroup.LogRemovingStudents";
            public const string LogRemovedStudentsSuccess = "InternshipGroup.LogRemovedStudentsSuccess";
            public const string LogRemoveStudentsFailed = "InternshipGroup.LogRemoveStudentsFailed";
            public const string LogRemoveStudentsError = "InternshipGroup.LogRemoveStudentsError";
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
            public const string RestoreSuccess = "StudentTerms.RestoreSuccess";
            public const string BulkWithdrawSuccess = "StudentTerms.BulkWithdrawSuccess";
            public const string ImportPreviewSuccess = "StudentTerms.ImportPreviewSuccess";
            public const string ImportConfirmSuccess = "StudentTerms.ImportConfirmSuccess";

            // Email notifications
            public const string EmailSubjectWithdraw = "StudentTerms.Email.SubjectWithdraw";
            public const string EmailBodyWithdraw = "StudentTerms.Email.BodyWithdraw";
            public const string EmailSubjectRestore = "StudentTerms.Email.SubjectRestore";
            public const string EmailBodyRestore = "StudentTerms.Email.BodyRestore";

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
            public const string LogRestored = "StudentTerms.Log.Restored";
            public const string LogBulkWithdrawn = "StudentTerms.Log.BulkWithdrawn";
            public const string LogImportConfirmed = "StudentTerms.Log.ImportConfirmed";
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
    }
}

