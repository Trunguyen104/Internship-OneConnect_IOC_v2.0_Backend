using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Commands.DeleteEnterprise
{
    public record DeleteEnterpriseCommand : IRequest<Result<DeleteEnterpriseResponse>>
    {
        public Guid EnterpriseId { get; set; }
    }
}
