using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById;

public class GetProjectByIdHandler : IRequestHandler<GetProjectByIdQuery, Result<GetProjectByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;

    public GetProjectByIdHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _messageService = messageService;
    }

    public async Task<Result<GetProjectByIdResponse>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _unitOfWork.Repository<Project>().Query()
            .AsNoTracking()
            .Include(p => p.Internship).ThenInclude(i => i.Term)
            .Include(p => p.Internship).ThenInclude(i => i.Job).ThenInclude(j => j.Enterprise)
            .Include(p => p.Internship).ThenInclude(i => i.Student).ThenInclude(s => s.User).ThenInclude(u => u.UniversityUser).ThenInclude(uu => uu.University)
            .Include(p => p.Mentor).ThenInclude(m => m.User)
            .Include(p => p.Members).ThenInclude(m => m.Student).ThenInclude(s => s.User).ThenInclude(u => u.UniversityUser).ThenInclude(uu => uu.University)
            .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

        if (project == null)
        {
            return Result<GetProjectByIdResponse>.Failure("Không tìm thấy dự án.", ResultErrorType.NotFound);
        }

        var response = _mapper.Map<GetProjectByIdResponse>(project);

        var membersDto = project.Members
            .Select(m => _mapper.Map<ProjectMemberDto>(m))
            .OrderByDescending(m => m.Role == ProjectMemberRole.LEADER.ToString())
            .ThenBy(m => m.FullName)
            .Select((m, index) =>
            {
                m.No = index + 1;
                return m;
            })
            .ToList();

        response.Members = membersDto;

        return Result<GetProjectByIdResponse>.Success(response);
    }
}
