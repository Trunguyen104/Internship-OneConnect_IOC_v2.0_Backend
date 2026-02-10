using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.DeleteStakeholderIssue;

public record DeleteStakeholderIssueCommand(Guid Id) : IRequest<Result<string>>;
