using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup
{
    public class AddStudentsToGroupHandler : IRequestHandler<AddStudentsToGroupCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public AddStudentsToGroupHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(AddStudentsToGroupCommand request, CancellationToken cancellationToken)
        {
            var group = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(g => g.Members)
                .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

            if (group == null)
            {
                return Result<Guid>.NotFound($"Không tìm thấy nhóm thực tập với ID {request.InternshipId}");
            }

            // Lọc bỏ danh sách trùng lặp truyền vào từ Request JSON
            var distinctInputs = request.Students.GroupBy(s => s.StudentId).Select(g => g.First()).ToList();

            foreach (var item in distinctInputs)
            {
                // Kiểm tra xem Group này đã tồn tại StudentId đó ở List Navigation chưa. Nếu chưa mới cho Add
                if (!group.Members.Any(m => m.StudentId == item.StudentId))
                {
                    group.Members.Add(new InternshipStudent
                    {
                        StudentId = item.StudentId,
                        InternshipId = group.InternshipId,
                        Role = item.Role,
                        Status = InternshipStatus.Registered,
                        JoinedAt = DateTime.UtcNow
                    });
                }
            }

            var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);
            if (saved >= 0)
            {
                return Result<Guid>.Success(group.InternshipId);
            }

            return Result<Guid>.Failure("Có lỗi xảy ra khi gán sinh viên vào nhóm.");
        }
    }
}
