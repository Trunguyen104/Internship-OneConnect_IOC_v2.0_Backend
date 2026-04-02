namespace IOCv2.Application.Common.Models
{
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>Danh sách lỗi phẳng cho exception không có field cụ thể.</summary>
        public List<string>? Errors { get; set; }

        /// <summary>Lỗi validation theo field (field-keyed). Ví dụ: {"file": ["Chỉ hỗ trợ file Excel XLSX"]}.</summary>
        public Dictionary<string, List<string>>? ValidationErrors { get; set; }

        public ErrorResponse(int statusCode, string message, List<string>? errors = null)
        {
            StatusCode = statusCode;
            Message = message;
            Errors = errors;
        }

        public ErrorResponse(int statusCode, string message, Dictionary<string, List<string>> validationErrors)
        {
            StatusCode = statusCode;
            Message = message;
            ValidationErrors = validationErrors;
        }
    }
}
