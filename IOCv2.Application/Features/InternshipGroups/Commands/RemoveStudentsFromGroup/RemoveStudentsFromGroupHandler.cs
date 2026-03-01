using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

using IOCv2.Application.Constants;
using AutoMapper;

namespace IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup
{
    public class RemoveStudentsFromGroupHandler : IRequestHandler<RemoveStudentsFromGroupCommand, Result<RemoveStudentsFromGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;

        // Dependency Injection UnitOfWork pattern
        public RemoveStudentsFromGroupHandler(IUnitOfWork unitOfWork, IMessageService messageService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
        }

        public async Task<Result<RemoveStudentsFromGroupResponse>> Handle(RemoveStudentsFromGroupCommand request, CancellationToken cancellationToken)
        {
            var group = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(g => g.Members)
                .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

            if (group == null)
            {
                return Result<RemoveStudentsFromGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
            }

            var repoStudent = _unitOfWork.Repository<InternshipStudent>();
            bool hasChanges = false;

            // Tiến hành quét từng StudentId. Ai bị nêu tên thì Delete record đó trong List Members
            foreach (var sId in request.StudentIds)
            {
                var targetMember = group.Members.FirstOrDefault(m => m.StudentId == sId);
                if (targetMember != null)
                {
                    await repoStudent.DeleteAsync(targetMember);
                    hasChanges = true;
                }
            }

            if (!hasChanges)
            {
                var responseNoChange = _mapper.Map<RemoveStudentsFromGroupResponse>(group);
                return Result<RemoveStudentsFromGroupResponse>.Success(responseNoChange); // Dù không thay đổi gì thì thao tác vẫn có thể coi là thành công (người cần xóa không ở trong nhóm)
            }

            var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);
            if (saved > 0)
            {
                var response = _mapper.Map<RemoveStudentsFromGroupResponse>(group);
                return Result<RemoveStudentsFromGroupResponse>.Success(response);
            }

            return Result<RemoveStudentsFromGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
        }
    }
}
