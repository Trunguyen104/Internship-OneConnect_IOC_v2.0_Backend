namespace IOCv2.Application.Common.Models
{
    public enum ResultErrorType
    {
        None = 1,
        BadRequest = 2,
        NotFound = 3,
        Unauthorized = 4,
        Forbidden = 5,
        Conflict = 6,
        InternalServerError = 7,
        TooManyRequests = 8
    }

    public class Result<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
        public ResultErrorType ErrorType { get; set; }
        public string? Warning { get; set; }
        public string? Message { get; set; }
        public bool HasWarning => !string.IsNullOrEmpty(Warning);

        public Result() { }

        private Result(bool isSuccess, T? data, string? error, ResultErrorType errorType, string? warning = null, string? message = null)
        {
            IsSuccess = isSuccess;
            Data = data;
            Error = error;
            ErrorType = errorType;
            Warning = warning;
            Message = message;
        }

        public static Result<T> Success(T data) => new(true, data, null, ResultErrorType.None);

        public static Result<T> Success(T data, string message) => new(true, data, null, ResultErrorType.None, message: message);

        public static Result<T> SuccessWithWarning(T data, string warning)
            => new(true, data, null, ResultErrorType.None, warning);

        public static Result<T> Failure(string error, ResultErrorType errorType = ResultErrorType.BadRequest)
            => new(false, default, error, errorType);

        public static Result<T> NotFound(string error) => Failure(error, ResultErrorType.NotFound);
    }
}

