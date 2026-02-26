using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup
{
    public class DeleteInternshipGroupHandler : IRequestHandler<DeleteInternshipGroupCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteInternshipGroupHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(DeleteInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(g => g.Members) // Đi kèm dữ liệu sinh viên để dọn rác
                .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

            if (entity == null)
            {
                return Result<Guid>.NotFound($"Không tìm thấy nhóm thực tập với ID {request.InternshipId}");
            }

            // Entity Framework Core có thể tự Cascade Delete nếu config chuẩn. 
            // Nếu không chuẩn, mình xóa chay trước các dữ liệu con Members
            if (entity.Members.Any())
            {
                var memberRepo = _unitOfWork.Repository<InternshipStudent>();
                foreach (var member in entity.Members.ToList())
                {
                    await memberRepo.DeleteAsync(member);
                }
            }

            // Xóa Nhóm cha
            await _unitOfWork.Repository<InternshipGroup>().DeleteAsync(entity);
            var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

            if (saved > 0)
            {
                return Result<Guid>.Success(entity.InternshipId);
            }

            return Result<Guid>.Failure("Không thể xóa nhóm thực tập và dữ liệu sinh viên đi kèm.");
        }
    }
}
