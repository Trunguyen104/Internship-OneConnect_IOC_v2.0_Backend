using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Logbooks.Commands.CreateLogbook;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook
{
    public class UpdateLogbookHandler : IRequestHandler<UpdateLogbookCommand, Result<UpdateLogbookResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateLogbookHandler> _logger;

        public UpdateLogbookHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService, ILogger<CreateLogbookHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public Task<Result<UpdateLogbookResponse>> Handle(UpdateLogbookCommand request, CancellationToken cancellationToken)
        {
            //Validate current user
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                
            }
        }
    }
}
