namespace IOCv2.Application.Common.Models
{
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string>? Errors { get; set; }

        public ErrorResponse(int statusCode, string message, List<string>? errors = null)
        {
            StatusCode = statusCode;
            Message = message;
            Errors = errors;
        }


    }
}
