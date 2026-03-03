using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

using IOCv2.Application.Constants;
using AutoMapper;

namespace IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup
{
    public class AddStudentsToGroupHandler : IRequestHandler<AddStudentsToGroupCommand, Result<AddStudentsToGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;

        public AddStudentsToGroupHandler(IUnitOfWork unitOfWork, IMessageService messageService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
        }

        public async Task<Result<AddStudentsToGroupResponse>> Handle(AddStudentsToGroupCommand request, CancellationToken cancellationToken)
        {
            var group = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(g => g.Members)
                .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

            if (group == null)
            {
                return Result<AddStudentsToGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
            }

            // Lọc bỏ danh sách trùng lặp truyền vào từ Request JSON
            var distinctInputs = request.Students.GroupBy(s => s.StudentId).Select(g => g.First()).ToList();
            
            // Validate xem các StudentId truyền vào có thực sự tồn tại trong hệ thống DB không
            var inputStudentIds = distinctInputs.Select(x => x.StudentId).ToList();

            var existingStudentIds = await _unitOfWork.Repository<Student>().Query()
                .Where(s => inputStudentIds.Contains(s.StudentId))
                .Select(s => s.StudentId)
                .ToListAsync(cancellationToken);

            if (existingStudentIds.Count != distinctInputs.Count)
            {
                return Result<AddStudentsToGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.InvalidStudentId));
            }

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
                var response = _mapper.Map<AddStudentsToGroupResponse>(group);
                return Result<AddStudentsToGroupResponse>.Success(response);
            }

            return Result<AddStudentsToGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
        }
    }
}
