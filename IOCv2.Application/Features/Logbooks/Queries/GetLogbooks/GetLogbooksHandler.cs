using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbooks
{
    public record GetLogbooksHandler : IRequestHandler<GetLogbooksQuery, Result<PaginatedResult<GetLogbooksResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetLogbooksHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public Task<Result<PaginatedResult<GetLogbooksResponse>>> Handle(GetLogbooksQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Repository<Logbook>().Query()
            .Include(x => x.Internship).ThenInclude(x => x.Student);

            // Filter by status
            if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<LogbookStatus>(request.Status, true, out var parsedStatus))
            {
                query = query.Where(u => u.Status == parsedStatus);
            }

            // Sorting
            query = (request.SortColumn?.ToLower(), request.SortOrder?.ToLower()) switch
            {
                ("studentname", "desc") => query.OrderByDescending(x => x.Internship.Student.FullName),
                ("studentname", _) => query.OrderBy(x => x.Internship.Student.FullName),
                ("internshipid", "desc") => query.OrderByDescending(x => x.InternshipId),
                ("internshipid", _) => query.OrderBy(x => x.InternshipId),
                ("createdat", "desc") => query.OrderByDescending(u => u.CreatedAt),
                ("createdat", _) => query.OrderBy(u => u.CreatedAt),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);

            var logbooks = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<GetLogbooksResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var result = PaginatedResult<GetLogbooksResponse>.Create(logbooks, totalCount, request.PageNumber, request.PageSize);
            return Result<PaginatedResult<GetLogbooksResponse>>.Success(result);
        }

    }
}
