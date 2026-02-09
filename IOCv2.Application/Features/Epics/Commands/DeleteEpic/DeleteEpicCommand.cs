using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Epics.Commands.DeleteEpic;

public record DeleteEpicCommand(Guid EpicId) : IRequest<Result>;
