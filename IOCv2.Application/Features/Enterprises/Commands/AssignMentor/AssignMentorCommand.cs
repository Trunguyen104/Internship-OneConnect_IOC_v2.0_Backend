using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Enterprises.Commands.AssignMentor;

public record AssignMentorCommand : IRequest<Result<AssignMentorResponse>>
{
    public Guid ApplicationId { get; init; }
    public Guid MentorEnterpriseUserId { get; init; }
}
