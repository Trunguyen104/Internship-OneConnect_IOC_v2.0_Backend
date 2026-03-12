using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Terms.Commands.DeleteTerm;

public record DeleteTermCommand(Guid TermId) : IRequest<Result<DeleteTermResponse>>;