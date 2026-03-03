using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

using IOCv2.Application.Constants;
using AutoMapper;

namespace IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup
{
    public class UpdateInternshipGroupHandler : IRequestHandler<UpdateInternshipGroupCommand, Result<UpdateInternshipGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;

        public UpdateInternshipGroupHandler(IUnitOfWork unitOfWork, IMessageService messageService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
        }

        public async Task<Result<UpdateInternshipGroupResponse>> Handle(UpdateInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

            if (entity == null)
            {
                return Result<UpdateInternshipGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
            }
            // Validate TermId
            var termExists = await _unitOfWork.Repository<Term>()
                .ExistsAsync(t => t.TermId == request.TermId, cancellationToken);
            if (!termExists)
            {
                return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.TermNotFound), ResultErrorType.NotFound);
            }

            // Validate EnterpriseId if provided
            if (request.EnterpriseId.HasValue)
            {
                var enterpriseExists = await _unitOfWork.Repository<Enterprise>()
                    .ExistsAsync(e => e.EnterpriseId == request.EnterpriseId.Value, cancellationToken);
                if (!enterpriseExists)
                {
                    return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseNotFound), ResultErrorType.NotFound);
                }
            }

            // Validate MentorId if provided
            if (request.MentorId.HasValue)
            {
                var mentorExists = await _unitOfWork.Repository<User>()
                    .ExistsAsync(u => u.UserId == request.MentorId.Value, cancellationToken);
                if (!mentorExists)
                {
                    return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.MentorNotFound), ResultErrorType.NotFound);
                }
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
                var response = _mapper.Map<UpdateInternshipGroupResponse>(entity);
                return Result<UpdateInternshipGroupResponse>.Success(response);
            }

            return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
        }
    }
}
