using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetAvailableMentors;

public record GetAvailableMentorsQuery : IRequest<Result<List<AvailableMentorDto>>>
{
    public Guid InternshipGroupId { get; init; }
    public string? SearchTerm { get; init; }
}
