using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById
{
    public class GetProjectByIdHandler : IRequestHandler<GetProjectByIdQuery, Result<GetProjectByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetProjectByIdHandler> _logger;
        private readonly IMessageService _message;
        public GetProjectByIdHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetProjectByIdHandler> logger, IMessageService message)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _message = message;
        }
        public async Task<Result<GetProjectByIdResponse>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
        {
            try {
                var project = await _unitOfWork.Repository<Domain.Entities.Project>()
                    .GetByIdAsync(request.ProjectId, cancellationToken);
                if (project == null)
                {
                    return Result<GetProjectByIdResponse>.Failure(
                        _message.GetMessage(MessageKeys.Projects.NotFound, request.ProjectId),
                        ResultErrorType.NotFound);
                }
                var response = _mapper.Map<GetProjectByIdResponse>(project);
                return Result<GetProjectByIdResponse>.Success(response);
            } catch (Exception ex) {
                _logger.LogError(ex, _message.GetMessage(MessageKeys.Projects.LogGetByIdError), request.ProjectId);
                return Result<GetProjectByIdResponse>.Failure(
                    _message.GetMessage(MessageKeys.Projects.LogGetByIdError, request.ProjectId),
                    ResultErrorType.BadRequest);
            }
        }
    }
}
