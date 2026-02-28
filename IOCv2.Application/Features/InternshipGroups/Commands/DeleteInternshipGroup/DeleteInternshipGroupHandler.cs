using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

using IOCv2.Application.Constants;
using AutoMapper;

namespace IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup
{
    public class DeleteInternshipGroupHandler : IRequestHandler<DeleteInternshipGroupCommand, Result<DeleteInternshipGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;

        public DeleteInternshipGroupHandler(IUnitOfWork unitOfWork, IMessageService messageService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
        }

        public async Task<Result<DeleteInternshipGroupResponse>> Handle(DeleteInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(g => g.Members) // Đi kèm dữ liệu sinh viên để dọn rác
                .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

            if (entity == null)
            {
                return Result<DeleteInternshipGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
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
                var response = _mapper.Map<DeleteInternshipGroupResponse>(entity);
                return Result<DeleteInternshipGroupResponse>.Success(response);
            }

            return Result<DeleteInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
        }
    }
}
