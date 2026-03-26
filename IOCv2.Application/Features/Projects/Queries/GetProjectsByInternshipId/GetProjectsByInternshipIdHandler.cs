using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectsByInternshipId
{
    public class GetProjectsByInternshipIdHandler : IRequestHandler<GetProjectsByInternshipIdQuery, Result<PaginatedResult<GetProjectsByInternshipIdResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetProjectsByInternshipIdHandler> _logger;

        public GetProjectsByInternshipIdHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService, ILogger<GetProjectsByInternshipIdHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<PaginatedResult<GetProjectsByInternshipIdResponse>>> Handle(GetProjectsByInternshipIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogGetByInternshipId), request.InternshipId);

            // Check if the internship exists
            var internshipExists = await _unitOfWork.Repository<InternshipGroup>()
                .ExistsAsync(i => i.InternshipId == request.InternshipId, cancellationToken);
            
            if (!internshipExists) 
            { 
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNotFound), request.InternshipId);
                return Result<PaginatedResult<GetProjectsByInternshipIdResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Internships.NotFound), ResultErrorType.NotFound); 
            }

                // Base query
                var query = _unitOfWork.Repository<Project>().Query()
                .Where(p => p.InternshipId == request.InternshipId)
                .AsNoTracking();

                // Apply search term
                if (!string.IsNullOrWhiteSpace(request.SearchTerm)) { var term = request.SearchTerm.Trim().ToLower(); 
                    query = query.Where(p => p.ProjectName.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term))); }

                // Apply status filter
                if (request.Status.HasValue) { query = query.Where(p => p.Status == request.Status.Value); }

                // Apply date range filter
                if (request.FromDate.HasValue) { query = query.Where(p => p.StartDate >= request.FromDate.Value); }
                if (request.ToDate.HasValue) { query = query.Where(p => p.EndDate <= request.ToDate.Value); }

                // Get total count before pagination
                var totalCount = await query.CountAsync(cancellationToken);

                // Apply sorting
                query = ApplySorting(query, request.SortColumn, request.SortOrder);

                // Apply pagination and project to response
                var items = await query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
                .ProjectTo<GetProjectsByInternshipIdResponse>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken);
                var result = PaginatedResult<GetProjectsByInternshipIdResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);
                
                _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogGetByInternshipIdSuccess), items.Count, request.InternshipId);

                return Result<PaginatedResult<GetProjectsByInternshipIdResponse>>.Success(result);
        }

        private IQueryable<Project> ApplySorting(IQueryable<Project> query, string? sortColumn, string? sortOrder)
        {
            var isDescending = sortOrder?.ToLower() == "desc";

            return (sortColumn?.ToLower(), isDescending) switch
            {
                ("projectname", true) => query.OrderByDescending(p => p.ProjectName),
                ("projectname", false) => query.OrderBy(p => p.ProjectName),

                ("startdate", true) => query.OrderByDescending(p => p.StartDate),
                ("startdate", false) => query.OrderBy(p => p.StartDate),

                ("enddate", true) => query.OrderByDescending(p => p.EndDate),
                ("enddate", false) => query.OrderBy(p => p.EndDate),

                ("status", true) => query.OrderByDescending(p => p.Status),
                ("status", false) => query.OrderBy(p => p.Status),

                ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
                ("createdat", false) => query.OrderBy(p => p.CreatedAt),

                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }
    }
}
