using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Commands.MoveToInterviewing;

public record MoveToInterviewingCommand(Guid ApplicationId) : IRequest<Result<MoveToInterviewingResponse>>;
