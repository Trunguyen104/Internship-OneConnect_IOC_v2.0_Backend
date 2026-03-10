using IOCv2.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Commands.RestoreEnterprise
{
    public record RestoreEnterpriseCommand : MediatR.IRequest<Result<RestoreEnterpriseResponse>>
    {
        /// <summary>
        /// The unique identifier of the enterprise to restore.
        /// </summary>
        public Guid EnterpriseId { get; init; }
    }
}
