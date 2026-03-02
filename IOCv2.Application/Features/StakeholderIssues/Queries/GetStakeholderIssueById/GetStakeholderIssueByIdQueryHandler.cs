using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssueById
{
    public class GetStakeholderIssueByIdQueryHandler : IRequestHandler<GetStakeholderIssueByIdQuery, Result<GetStakeholderIssueByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public GetStakeholderIssueByIdQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<GetStakeholderIssueByIdResponse>> Handle(GetStakeholderIssueByIdQuery request, CancellationToken cancellationToken)
        {
            var issue = await _unitOfWork.Repository<StakeholderIssue>()
                .Query()
                .ProjectTo<GetStakeholderIssueByIdResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(si => si.Id == request.Id, cancellationToken);

            if (issue == null)
                return Result<GetStakeholderIssueByIdResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Issue.NotFound));

            return Result<GetStakeholderIssueByIdResponse>.Success(issue);
        }
    }
}
