using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.CreateStakeholderIssue
{
    public class CreateStakeholderIssueCommandHandler : IRequestHandler<CreateStakeholderIssueCommand, Result<CreateStakeholderIssueResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public CreateStakeholderIssueCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<CreateStakeholderIssueResponse>> Handle(CreateStakeholderIssueCommand request, CancellationToken cancellationToken)
        {
            // Check stakeholder exists
            var stakeholderExists = await _unitOfWork.Repository<Stakeholder>()
                .ExistsAsync(s => s.Id == request.StakeholderId, cancellationToken);

            if (!stakeholderExists)
                return Result<CreateStakeholderIssueResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Issue.StakeholderNotFound));

            // Create entity
            var issue = new StakeholderIssue
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                StakeholderId = request.StakeholderId,
                Status = StakeholderIssueStatus.Open
            };

            // Persist
            await _unitOfWork.Repository<StakeholderIssue>().AddAsync(issue, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Return response
            var response = _mapper.Map<CreateStakeholderIssueResponse>(issue);
            return Result<CreateStakeholderIssueResponse>.Success(
                response,
                _messageService.GetMessage(MessageKeys.Issue.CreateSuccess)
            );
        }
    }
}

