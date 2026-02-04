using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Common.Models
{
    /// <summary>
    /// Phân loại lỗi nghiệp vụ để map sang HTTP Status Code ở Presentation layer
    /// </summary>
    public enum ResultErrorType
    {
        None = 0,
        Validation = 1,     // Dữ liệu đầu vào sai
        BadRequest = 2,     // Request không hợp lệ
        NotFound = 3,       // Không tìm thấy resource
        Unauthorized = 4,   // Chưa đăng nhập / token sai
        Forbidden = 5,      // Không đủ quyền
        Conflict = 6        // Trùng dữ liệu / vi phạm business rule
    }

    /// <summary>
    /// Result KHÔNG trả dữ liệu (dùng cho Update / Delete / Command không cần response)
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public List<string> Errors { get; }
        public ResultErrorType ErrorType { get; }

        protected Result(
            bool isSuccess,
            List<string>? errors,
            ResultErrorType errorType)
        {
            IsSuccess = isSuccess;
            Errors = errors ?? new List<string>();
            ErrorType = errorType;
        }

        public static Result Success()
            => new(true, null, ResultErrorType.None);

        public static Result Failure(
            string error,
            ResultErrorType errorType = ResultErrorType.BadRequest)
            => new(false, new List<string> { error }, errorType);

        public static Result Failure(
            List<string> errors,
            ResultErrorType errorType = ResultErrorType.BadRequest)
            => new(false, errors, errorType);

        public static Result ValidationFailure(List<string> errors)
            => new(false, errors, ResultErrorType.Validation);

        public static Result NotFound(string error)
            => new(false, new List<string> { error }, ResultErrorType.NotFound);

        public static Result Conflict(string error)
            => new(false, new List<string> { error }, ResultErrorType.Conflict);
    }

    /// <summary>
    /// Result CÓ trả dữ liệu (dùng cho Query / Create)
    /// </summary>
    public class Result<T> : Result
    {
        public T? Data { get; }

        private Result(
            bool isSuccess,
            T? data,
            List<string>? errors,
            ResultErrorType errorType)
            : base(isSuccess, errors, errorType)
        {
            Data = data;
        }

        public static Result<T> Success(T data)
            => new(true, data, null, ResultErrorType.None);

        public static new Result<T> Failure(
            string error,
            ResultErrorType errorType = ResultErrorType.BadRequest)
            => new(false, default, new List<string> { error }, errorType);

        public static new Result<T> Failure(
            List<string> errors,
            ResultErrorType errorType = ResultErrorType.BadRequest)
            => new(false, default, errors, errorType);

        public static Result<T> ValidationFailure(List<string> errors)
            => new(false, default, errors, ResultErrorType.Validation);

        public static Result<T> NotFound(string error)
            => new(false, default, new List<string> { error }, ResultErrorType.NotFound);

        public static Result<T> Conflict(string error)
            => new(false, default, new List<string> { error }, ResultErrorType.Conflict);
    }
}

