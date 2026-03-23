namespace IOCv2.Application.Common.Exceptions
{
    /// <summary>
    /// Ném ra khi vi phạm quy tắc nghiệp vụ domain.
    /// Global Filter bắt và trả về HTTP 400 Bad Request.
    /// </summary>
    public class DomainViolationException : Exception
    {
        public DomainViolationException(string message) : base(message)
        {
        }
    }
}
