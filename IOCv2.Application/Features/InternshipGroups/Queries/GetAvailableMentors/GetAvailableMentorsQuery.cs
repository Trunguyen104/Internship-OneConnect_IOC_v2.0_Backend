using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetAvailableMentors;

public record GetAvailableMentorsQuery(Guid InternshipGroupId)
    : IRequest<Result<List<AvailableMentorDto>>>;
