using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.HRApplications.UniAssign.Commands.ApproveUniAssign;

public record ApproveUniAssignCommand(Guid ApplicationId) : IRequest<Result<ApproveUniAssignResponse>>;
