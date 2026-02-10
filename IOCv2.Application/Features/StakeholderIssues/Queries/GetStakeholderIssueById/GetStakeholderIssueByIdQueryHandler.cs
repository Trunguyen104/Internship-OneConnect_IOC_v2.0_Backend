using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.StakeholderIssues.DTOs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using IOCv2.Application.Resources;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssueById;

public class GetStakeholderIssueByIdQueryHandler : IRequestHandler<GetStakeholderIssueByIdQuery, Result<StakeholderIssueDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer;

    public GetStakeholderIssueByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IStringLocalizer<ErrorMessages> errorLocalizer)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _errorLocalizer = errorLocalizer;
    }

    public async Task<Result<StakeholderIssueDto>> Handle(GetStakeholderIssueByIdQuery request, CancellationToken cancellationToken)
    {
        var issue = await _unitOfWork.Repository<StakeholderIssue>().Query()
            .Include(si => si.Stakeholder)
            .ProjectTo<StakeholderIssueDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(si => si.Id == request.Id, cancellationToken);

        if (issue == null)
        {
            return Result<StakeholderIssueDto>.Failure(_errorLocalizer["Issue.NotFound"], ResultErrorType.NotFound);
        }

        return Result<StakeholderIssueDto>.Success(issue);
    }
}
