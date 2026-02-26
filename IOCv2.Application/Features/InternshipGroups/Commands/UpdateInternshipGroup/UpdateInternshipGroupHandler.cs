using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup
{
    public class UpdateInternshipGroupHandler : IRequestHandler<UpdateInternshipGroupCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateInternshipGroupHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(UpdateInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

            if (entity == null)
            {
                return Result<Guid>.NotFound($"Không tìm thấy nhóm thực tập với ID {request.InternshipId}");
            }

            entity.TermId = request.TermId;
            entity.GroupName = request.GroupName;
            entity.EnterpriseId = request.EnterpriseId;
            entity.MentorId = request.MentorId;
            entity.StartDate = request.StartDate;
            entity.EndDate = request.EndDate;

            await _unitOfWork.Repository<InternshipGroup>().UpdateAsync(entity);
            var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

            if (saved > 0 || !_unitOfWork.Repository<InternshipGroup>().Query().Any(x => x.InternshipId == request.InternshipId))
            {
                // Accept no DB changes if content is same
                return Result<Guid>.Success(entity.InternshipId);
            }

            return Result<Guid>.Failure("Cập nhật nhóm thực tập không thành công.");
        }
    }
}
