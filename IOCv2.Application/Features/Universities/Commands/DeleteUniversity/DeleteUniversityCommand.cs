using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Universities.Commands.DeleteUniversity;

public record DeleteUniversityCommand(Guid UniversityId) : IRequest<Result<bool>>;
