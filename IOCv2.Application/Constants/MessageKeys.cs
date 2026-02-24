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


        public static class Validation
        {
            public const string NameMaxLength = "Validation.NameMaxLength";
            public const string DescriptionMaxLength = "Validation.DescriptionMaxLength";
            public const string IdRequired = "Validation.IdRequired";
        }
    }
}
