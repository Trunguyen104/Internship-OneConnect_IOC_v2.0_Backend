using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Universities.Queries.GetUniversities;

public class GetUniversitiesResponse
{
    /// <summary>
    /// Unique identifier for the university.
    /// </summary>
    public Guid UniversityId { get; set; }

    /// <summary>
    /// Official code of the university.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the university.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Physical address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// URL to the university logo thumbnail.
    /// </summary>
    public string? LogoUrl { get; set; }
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Current status (1=Inactive, 2=Active).
    /// </summary>
    public UniversityStatus Status { get; set; }

    public static GetUniversitiesResponse FromEntity(University university)
    {
        return new GetUniversitiesResponse
        {
            UniversityId = university.UniversityId,
            Code = university.Code,
            Name = university.Name,
            Address = university.Address,
            LogoUrl = university.LogoUrl,
            ContactEmail = university.ContactEmail,
            Status = university.Status
        };
    }
}
