using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Logbooks.Commands.CreateLogbook;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Commands.DeleteLogbook
{
    public record DeleteLogbookCommand : IRequest<Result<DeleteLogbookResponse>>
    {
        public Guid LogbookId { get; set; }
    }
}
