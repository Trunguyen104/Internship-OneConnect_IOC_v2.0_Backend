using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Commands.CreateLogbook
{
    public class CreateLogbookHandler : IRequestHandler<CreateLogbookCommand, Result<CreateLogbookResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateLogbookHandler> _logger;

        public CreateLogbookHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService, ILogger<CreateLogbookHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }   

        public async Task<Result<CreateLogbookResponse>> Handle(CreateLogbookCommand request, CancellationToken cancellationToken)
        {
        }
    }
}
