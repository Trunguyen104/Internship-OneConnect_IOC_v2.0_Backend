using AutoMapper;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Common.Models;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroupById
{
    public class GetInternshipGroupByIdHandler : IRequestHandler<GetInternshipGroupByIdQuery, Result<GetInternshipGroupByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetInternshipGroupByIdHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<GetInternshipGroupByIdResponse>> Handle(GetInternshipGroupByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(ig => ig.Enterprise)
                .Include(ig => ig.Mentor!).ThenInclude(m => m.User!)
                .Include(ig => ig.Members).ThenInclude(m => m.Student!).ThenInclude(s => s.User!).ThenInclude(u => u.UniversityUser!).ThenInclude(uu => uu.University!)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

            if (entity == null)
            {
                return Result<GetInternshipGroupByIdResponse>.NotFound($"Group with ID {request.InternshipId} is not found.");
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
