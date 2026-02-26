using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup
{
    public class RemoveStudentsFromGroupHandler : IRequestHandler<RemoveStudentsFromGroupCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;

        // Dependency Injection UnitOfWork pattern
        public RemoveStudentsFromGroupHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(RemoveStudentsFromGroupCommand request, CancellationToken cancellationToken)
        {
            var group = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(g => g.Members)
                .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

            if (group == null)
            {
                return Result<Guid>.NotFound($"Không tìm thấy nhóm thực tập với ID {request.InternshipId}");
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
                return Result<Guid>.Success(group.InternshipId); // Dù không thay đổi gì thì thao tác vẫn có thể coi là thành công (người cần xóa không ở trong nhóm)
            }

            var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);
            if (saved > 0)
            {
                return Result<Guid>.Success(group.InternshipId);
            }

            return Result<Guid>.Failure("Có lỗi xảy ra khi xóa sinh viên khỏi nhóm.");
        }
    }
}
