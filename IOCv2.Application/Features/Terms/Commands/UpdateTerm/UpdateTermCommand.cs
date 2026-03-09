using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Terms.Commands.UpdateTerm;

public record UpdateTermCommand : IRequest<Result<UpdateTermResponse>>
{
    public Guid TermId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int Version { get; init; }
}