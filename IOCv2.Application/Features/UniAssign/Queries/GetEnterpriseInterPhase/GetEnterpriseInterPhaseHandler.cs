using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.UniAssign.Queries.GetEnterpriseInterPhase
{
    public class GetEnterpriseInterPhaseHandler
        : MediatR.IRequestHandler<GetEnterpriseInterPhaseQuery, Result<List<GetEnterpriseInterPhaseResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetEnterpriseInterPhaseHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<List<GetEnterpriseInterPhaseResponse>>> Handle(
            GetEnterpriseInterPhaseQuery request,
            CancellationToken cancellationToken)
        {
            // Normalize search term — ToLower() translates to SQL LOWER(), safe for EF Core
            var search = request?.SearchTerm?.Trim().ToLower();
            var hasSearch = !string.IsNullOrWhiteSpace(search);

            var query = _unitOfWork.Repository<InternshipPhase>()
                .Query()
                .Include(e => e.Enterprise).AsQueryable();
            var currentUserUniversityId = await _unitOfWork.Repository<UniversityUser>()
                .Query()
                .Where(x => x.UserId == Guid.Parse(_currentUserService.UserId!))
                .Select(x => x.UniversityId)
                .FirstOrDefaultAsync(cancellationToken);

            query = query.Where(x => x.Enterprise != null &&
                    _unitOfWork.Repository<UniversityUser>()
                        .Query()
                        .Any(u => u.UniversityId == currentUserUniversityId && u.UserId == Guid.Parse(_currentUserService.UserId!)));
            // EF Core translates EF.Functions.Like or simple ToLower().Contains() to SQL
            if (hasSearch)
            {
                query = query.Where(x =>
                    x.Enterprise!.Name.ToLower().Contains(search!)
                    || x.Name.ToLower().Contains(search!));
            }

            var response = await query
                // Tính placedCount một lần duy nhất qua let-style projection
                .Select(x => new
                {
                    x.EnterpriseId,
                    EnterpriseName = x.Enterprise!.Name,
                    x.PhaseId,
                    x.Name,
                    x.MajorFields,
                    x.Capacity,
                    PlacedCount = x.Jobs
                        .SelectMany(j => j.InternshipApplications)
                        .Count(a => a.Status == InternshipApplicationStatus.Placed)
                })
                // Filter sau khi đã project — tránh tính lại 2 lần
                .Where(x => x.Capacity - x.PlacedCount > 0)
                .OrderBy(x => x.EnterpriseName)
                .ThenBy(x => x.Name)
                .Select(x => new GetEnterpriseInterPhaseResponse
                {
                    EnterpriseId = x.EnterpriseId,
                    EnterpriseName = x.EnterpriseName,
                    InternPhaseId = x.PhaseId,
                    InternPhaseName = x.Name,
                    MajorFields = x.MajorFields,
                    Capacity = x.Capacity,
                    RemainingCapacity = x.Capacity - x.PlacedCount
                })
                .ToListAsync(cancellationToken);

            return Result<List<GetEnterpriseInterPhaseResponse>>.Success(response);
        }
    }
}