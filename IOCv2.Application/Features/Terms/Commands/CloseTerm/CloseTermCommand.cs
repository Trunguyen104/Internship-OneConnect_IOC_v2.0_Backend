using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Terms.Commands.CloseTerm;

public record CloseTermCommand : IRequest<Result<CloseTermResponse>>
{
    public Guid TermId { get; init; }
    public int Version { get; init; }
    public string Reason { get; init; } = string.Empty;
}