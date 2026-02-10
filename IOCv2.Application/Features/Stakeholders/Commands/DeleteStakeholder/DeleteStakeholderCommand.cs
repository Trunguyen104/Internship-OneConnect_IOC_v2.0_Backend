using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder;

public record DeleteStakeholderCommand(Guid Id) : IRequest<Result>;


