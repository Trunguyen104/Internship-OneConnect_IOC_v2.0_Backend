using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Admin.UserManagement.Queries.GetUserById;
using IOCv2.Application.Features.Logbooks.Common;
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

namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbookById
{
    public class GetLogbookByIdHandler : IRequestHandler<GetLogbookByIdQuery, Result<GetLogbookByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetLogbookByIdHandler> _logger;
        private readonly ICacheService _cacheService;

        public GetLogbookByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<GetLogbookByIdHandler> logger,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<GetLogbookByIdResponse>> Handle(GetLogbookByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching logbook details for LogbookId: {LogbookId}", request.LogbookId);

            var cacheKey = LogbookCacheKeys.Logbook(request.LogbookId);
            var cached = await _cacheService.GetAsync<GetLogbookByIdResponse>(cacheKey, cancellationToken);
            if (cached is not null)
                return Result<GetLogbookByIdResponse>.Success(cached);

            var logbook = await _unitOfWork.Repository<Logbook>()
                .Query()
                .AsNoTracking()
                .Include(x => x.Student!)
                    .ThenInclude(s => s.User!)
                .Include(x => x.WorkItems)

                .FirstOrDefaultAsync(x => x.LogbookId == request.LogbookId, cancellationToken);


            if (logbook == null)
            {
                _logger.LogWarning("Logbook not found: {LogbookId}", request.LogbookId);
                return Result<GetLogbookByIdResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Logbooks.NotFound));
            }

            _logger.LogInformation("Successfully retrieved logbook {LogbookId}", request.LogbookId);

            var response = _mapper.Map<GetLogbookByIdResponse>(logbook);

            await _cacheService.SetAsync(cacheKey, response, LogbookCacheKeys.Expiration.Logbook, cancellationToken);

            return Result<GetLogbookByIdResponse>.Success(response);
        }
    }
}
