using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Terms.Commands.CreateTerm;

public record CreateTermCommand : IRequest<Result<CreateTermResponse>>
{
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }

    /// <summary>
    ///     Required when called by SuperAdmin to specify which university to create the term for.
    ///     SchoolAdmin does not need to provide this — it is resolved automatically.
    /// </summary>
    public Guid? UniversityId { get; init; }
}