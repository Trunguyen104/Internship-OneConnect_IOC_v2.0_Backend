using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<GetLogbooksHandler> _logger;

        public GetLogbooksHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetLogbooksHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedResult<GetLogbooksResponse>>> Handle(GetLogbooksQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Repository<Logbook>()
                        .Query()
                        .Include(x => x.Student!)
                            .ThenInclude(s => s.User!)
                        .Include(x => x.Project)
                        .Where(x => x.ProjectId == request.ProjectId)
                        .AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<LogbookStatus>(request.Status, true, out var parsedStatus))
            {
                query = query.Where(x => x.Status == parsedStatus);
            }

            // Sorting
            query = (request.SortColumn?.ToLower(), request.SortOrder?.ToLower()) switch
            {
                ("studentname", "desc") => query.OrderByDescending(x => x.Student!.User!.FullName),
                ("studentname", _) => query.OrderBy(x => x.Student!.User!.FullName),
                ("createdat", "desc") => query.OrderByDescending(x => x.CreatedAt),
                ("createdat", _) => query.OrderBy(x => x.CreatedAt),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);

            try
            {
                var logbooks = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ProjectTo<GetLogbooksResponse>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                var result = PaginatedResult<GetLogbooksResponse>.Create(logbooks, totalCount, request.PageNumber, request.PageSize);
                return Result<PaginatedResult<GetLogbooksResponse>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching logbooks");
                throw; // hoặc return Result<PaginatedResult<GetLogbooksResponse>>.Failure(ex.Message);
            }
        }

    }
}
