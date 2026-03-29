using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentApplications.Commands.HideApplication;

public record HideApplicationCommand(Guid ApplicationId) : IRequest<Result<HideApplicationResponse>>;

public class HideApplicationResponse
{
    public Guid ApplicationId { get; set; }
    public string Message { get; set; } = string.Empty;
}
