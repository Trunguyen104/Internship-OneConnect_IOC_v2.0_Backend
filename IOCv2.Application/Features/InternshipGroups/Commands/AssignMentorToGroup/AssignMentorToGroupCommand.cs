using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Commands.AssignMentorToGroup;

public record AssignMentorToGroupCommand(
    Guid InternshipGroupId,
    Guid MentorUserId           // UserId của mentor (frontend gửi UserId, không phải EnterpriseUserId)
) : IRequest<Result<AssignMentorToGroupResponse>>;
