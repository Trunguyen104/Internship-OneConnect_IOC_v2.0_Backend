using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Enterprises.Commands.AcceptApplication;

public record AcceptApplicationCommand(Guid ApplicationId) : IRequest<Result<AcceptApplicationResponse>>;
