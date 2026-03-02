using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbookById
{
    public class GetLogbookByIdQuery : IRequest<Result<GetLogbookByIdResponse>>
    {
        public Guid LogbookId { get; set; }
    }
}
