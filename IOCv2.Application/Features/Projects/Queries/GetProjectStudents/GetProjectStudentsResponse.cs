namespace IOCv2.Application.Features.Projects.Queries.GetProjectStudents
{
    public class GetProjectStudentsResponse
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? ClassName { get; set; }
    }
}

