﻿﻿namespace IOCv2.Application.Constants
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
            public const string AccessDenied = "Common.AccessDenied";
            public const string NotFound = "Common.NotFound";
            public const string RecordNotFound = "Common.RecordNotFound";
            public const string InvalidRequest = "Common.InvalidRequest";
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
            public const string DuplicateTaxCode = "Enterprise.DuplicateTaxCode";
            public const string Unverified = "Enterprise.Unverified";
            public const string Inactive = "Enterprise.Inactive";
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
        }

        public static class Internships
        {
            public const string NotFound = "Internships.NotFound";
            public const string InternshipIdRequired = "Internships.InternshipIdRequired";  
        }

        public static class Logbook
        {
            public const string NotFound = "Logbook.NotFound";
            public const string InvalidInternship = "Logbook.InvalidInternship";
            public const string CreationFailed = "Logbook.CreationFailed";
            public const string UpdateFailed = "Logbook.UpdateFailed";
            public const string DeleteFailed = "Logbook.DeleteFailed";
            public const string AlreadyReported = "Logbook.AlreadyReported";
        }

        public static class Page {
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
    }
}
