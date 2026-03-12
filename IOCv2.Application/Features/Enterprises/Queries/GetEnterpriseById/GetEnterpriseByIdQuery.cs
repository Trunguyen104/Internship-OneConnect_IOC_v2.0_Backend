using IOCv2.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseById
{
    public record GetEnterpriseByIdQuery : MediatR.IRequest<Result<GetEnterpriseByIdResponse>>
    {
        /// <summary>
        /// The unique identifier of the enterprise to retrieve.
        /// </summary>
        public Guid Id { get; init; }
    }
}
