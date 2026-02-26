using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup
{
    public class CreateInternshipGroupHandler : IRequestHandler<CreateInternshipGroupCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateInternshipGroupHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(CreateInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            var newGroup = new InternshipGroup
            {
                InternshipId = Guid.NewGuid(),
                TermId = request.TermId,
                GroupName = request.GroupName,
                EnterpriseId = request.EnterpriseId,
                MentorId = request.MentorId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = InternshipStatus.Registered
            };

            // Nếu có gửi kèm danh sách sinh viên thì nạp luôn vào
            if (request.Students != null && request.Students.Any())
            {
                // Loại bỏ sinh viên trùng lặp trong request
                var distinctStudents = request.Students
                    .GroupBy(s => s.StudentId)
                    .Select(g => g.First())
                    .ToList();

                foreach (var studentRef in distinctStudents)
                {
                    // (Optional) Có thể gọi .AnyAsync() để check xem StudentId có trong DB không nếu cần
                    newGroup.Members.Add(new InternshipStudent
                    {
                        StudentId = studentRef.StudentId,
                        InternshipId = newGroup.InternshipId, // EF tự map khóa ngoại
                        Role = studentRef.Role,
                        Status = InternshipStatus.Registered,
                        JoinedAt = DateTime.UtcNow
                    });
                }
            }

            await _unitOfWork.Repository<InternshipGroup>().AddAsync(newGroup);

            var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);
            if (saved > 0)
            {
                return Result<Guid>.Success(newGroup.InternshipId);
            }

            return Result<Guid>.Failure("Có lỗi xảy ra khi lưu nhóm vào Cơ sở dữ liệu.");
        }
    }
}
