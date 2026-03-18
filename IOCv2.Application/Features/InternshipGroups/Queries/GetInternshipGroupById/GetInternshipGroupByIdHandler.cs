using AutoMapper;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Common.Models;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IOCv2.Application.Constants;

using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroupById
{
    public class GetInternshipGroupByIdHandler : IRequestHandler<GetInternshipGroupByIdQuery, Result<GetInternshipGroupByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetInternshipGroupByIdHandler> _logger;

        public GetInternshipGroupByIdHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService, ILogger<GetInternshipGroupByIdHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetInternshipGroupByIdResponse>> Handle(GetInternshipGroupByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Querying internship group with ID: {Id}", request.InternshipId);
            var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(ig => ig.Enterprise)
                .Include(ig => ig.Mentor!).ThenInclude(m => m.User!)
                .Include(ig => ig.Members).ThenInclude(m => m.Student!).ThenInclude(s => s.User!).ThenInclude(u => u.UniversityUser!).ThenInclude(uu => uu.University!)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

            if (entity == null)
            {
                return Result<GetInternshipGroupByIdResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
            }

            var result = _mapper.Map<GetInternshipGroupByIdResponse>(entity);

            // Sắp xếp lại danh sách theo Leader lên đầu
            if (result.Members != null && result.Members.Any())
            {
                result.Members = result.Members.OrderByDescending(m => m.Role == Domain.Enums.InternshipRole.Leader).ToList();
            }

            return Result<GetInternshipGroupByIdResponse>.Success(result);
        }
    }
}
