using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Commands.RestoreStudent;

public record RestoreStudentCommand(Guid StudentTermId) : IRequest<Result<RestoreStudentResponse>>;

public class RestoreStudentResponse
{
    public Guid StudentTermId { get; set; }
}
