﻿namespace IOCv2.Application.Common.Models
{
    public enum ResultErrorType
    {
        None,
        BadRequest,
        NotFound,
        Unauthorized,
        Forbidden,
        Conflict,
        InternalServerError
    }

    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? Error { get; private set; }
        public ResultErrorType ErrorType { get; private set; }
        public string? Warning { get; private set; }
        public string? Message { get; private set; }
        public bool HasWarning => !string.IsNullOrEmpty(Warning);

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

