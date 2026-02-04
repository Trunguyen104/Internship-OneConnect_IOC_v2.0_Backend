namespace IOCv2.Application.Constants
{
    public static class MessageKeys
    {
        public static class Common
        {
            public const string DatabaseConflict = "DatabaseConflict";
            public const string DatabaseUpdateError = "DatabaseUpdateError";
            public const string OperationCancelled = "OperationCancelled";
            public const string Unauthorized = "Unauthorized";
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
            public const string OnlyManagerCanReset = "OnlyManagerCanReset";
            public const string OnlyActiveEmployeeCanReset = "OnlyActiveEmployeeCanReset";
            public const string ReasonRequired = "ResetPassword.ReasonRequired";
            public const string ReasonMinLength = "ResetPassword.ReasonMinLength";
            public const string ReasonMaxLength = "ResetPassword.ReasonMaxLength";
            public const string SuccessWithEmail = "ResetPassword.SuccessWithEmail";
            public const string SuccessNoEmail = "ResetPassword.SuccessNoEmail";
            public const string StatusNoEmail = "ResetStatusNoEmail";
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
            public const string EmployeeCodeRequired = "Auth.EmployeeCodeRequired";
        }
        // mẫu
        public static class Employee
        {
            public const string NotFound = "EmployeeNotFound";
            public const string NotActive = "EmployeeNotActive";
            public const string CannotUpdateInactive = "Employee.CannotUpdateInactive";
            public const string CannotIdentifyUser = "CannotIdentifyUser";
            public const string CannotIdentifyManager = "CannotIdentifyManager";
            public const string CannotPromoteToManager = "CannotPromoteToManager";
            public const string NewRoleMustBeDifferent = "NewRoleMustBeDifferent";
            public const string RoleChangedButEmailFailed = "RoleChangedButEmailFailed";
            public const string CodeInvalidFormat = "Employee.CodeInvalidFormat";
        }

        public static class Profile
        {
            public const string UsernameExists = "Profile.UsernameExists";
            public const string PhoneExists = "Profile.PhoneExists";
            public const string EmailExists = "Profile.EmailExists";
            public const string EmployeeIdRequired = "Profile.EmployeeIdRequired";
            public const string FullNameRequired = "Profile.FullNameRequired";
            public const string FullNameMaxLength = "Profile.FullNameMaxLength";
            public const string EmailRequired = "Profile.EmailRequired";
            public const string EmailInvalid = "Profile.EmailInvalid";
            public const string PhoneRequired = "Profile.PhoneRequired";
            public const string PhoneInvalid = "Profile.PhoneInvalid";
        }
    }
}
