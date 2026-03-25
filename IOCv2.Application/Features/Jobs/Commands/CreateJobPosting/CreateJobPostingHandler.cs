using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using AutoMapper;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace IOCv2.Application.Features.Jobs.Commands.CreateJobPosting
{
    public class CreateJobPostingHandler : IRequestHandler<CreateJobPostingCommand, Result<CreateJobPostingResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateJobPostingHandler> _logger;
        private readonly ICacheService _cacheService;

        public CreateJobPostingHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMapper mapper, ILogger<CreateJobPostingHandler> logger, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<CreateJobPostingResponse>> Handle(CreateJobPostingCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return Result<CreateJobPostingResponse>.Failure("Job title is required.", ResultErrorType.BadRequest);
                }

                if (request.ExpireDate.HasValue && request.ExpireDate.Value.Date < DateTime.UtcNow.Date)
                {
                    return Result<CreateJobPostingResponse>.Failure("Expire date must be today or later.", ResultErrorType.BadRequest);
                }

                // Resolve enterprise id from current user's UnitId (HR context)
                if (string.IsNullOrWhiteSpace(_currentUserService.UnitId) || !Guid.TryParse(_currentUserService.UnitId, out var enterpriseId))
                {
                    return Result<CreateJobPostingResponse>.Failure("Unable to determine enterprise for current user.", ResultErrorType.Unauthorized);
                }

                // Create domain job (status = DRAFT)
                var job = Job.Create(
                    enterpriseId: enterpriseId,
                    title: request.Title,
                    description: request.Description,
                    requirements: request.Requirements,
                    benefit: request.Benefit,
                    location: request.Location,
                    quantity: request.Quantity,
                    expireDate: request.ExpireDate
                );

                // Set audit fields
                if (!string.IsNullOrWhiteSpace(_currentUserService.UserId) && Guid.TryParse(_currentUserService.UserId, out var userId))
                {
                    job.CreatedBy = userId;
                }

                // Persist
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                var repo = _unitOfWork.Repository<Job>();
                await repo.AddAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Map to response DTO
                var dto = _mapper.Map<CreateJobPostingResponse>(job);

                // Return success with toast message expected by UI
                return Result<CreateJobPostingResponse>.Success(dto, "Đã lưu bản nháp.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job posting draft");
                try
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                }
                catch { /* swallow rollback exceptions */ }

                return Result<CreateJobPostingResponse>.Failure("Internal server error while creating job.", ResultErrorType.InternalServerError);
            }
        }
    }
}
