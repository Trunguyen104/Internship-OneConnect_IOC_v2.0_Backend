using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Admin.Users.Queries.GetAdminUserById;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        public GetLogbookByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<GetLogbookByIdResponse>> Handle(GetLogbookByIdQuery request, CancellationToken cancellationToken)
        {
            var logbook = await _unitOfWork.Repository<Logbook>()
            .Query()
            .Include(x => x.Student)
                .ThenInclude(s => s.User)
            .Include(x => x.WorkItem)
            .FirstOrDefaultAsync(x => x.LogbookId == request.LogbookId, cancellationToken);

            if (logbook == null)
            {
                return Result<GetLogbookByIdResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Logbook.NotFound));
            }

            var response = _mapper.Map<GetLogbookByIdResponse>(logbook);

            return Result<GetLogbookByIdResponse>.Success(response);
        }
    }
}
