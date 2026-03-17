namespace IOCv2.Application.Features.Universities.Queries.GetUniversityById;

public class GetUniversityByIdResponse
{
    public Guid UniversityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public short Status { get; set; }
}
