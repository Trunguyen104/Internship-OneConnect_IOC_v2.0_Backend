using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssueById
{
    public class GetStakeholderIssueByIdQueryHandler : IRequestHandler<GetStakeholderIssueByIdQuery, Result<GetStakeholderIssueByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetStakeholderIssueByIdQueryHandler> _logger;

        public GetStakeholderIssueByIdQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<GetStakeholderIssueByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetStakeholderIssueByIdResponse>> Handle(GetStakeholderIssueByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting StakeholderIssue {Id}", request.Id);

            var issue = await _unitOfWork.Repository<StakeholderIssue>()
            .Query()
            .AsNoTracking()
            .ProjectTo<GetStakeholderIssueByIdResponse>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(si => si.Id == request.Id, cancellationToken);

            if (issue == null)
            {
                _logger.LogWarning("StakeholderIssue {Id} not found", request.Id);
                return Result<GetStakeholderIssueByIdResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Issue.NotFound));
            }

            _logger.LogInformation("Successfully retrieved StakeholderIssue {Id}", request.Id);
            return Result<GetStakeholderIssueByIdResponse>.Success(issue);

        }
    }
}
