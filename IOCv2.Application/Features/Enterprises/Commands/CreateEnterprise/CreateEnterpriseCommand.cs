using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Commands.CreateEnterprise
{
    public record CreateEnterpriseCommand : IRequest<Result<CreateEnterpriseResponse>>
    {
        public string? TaxCode { get; set; }
        public string Name { get; set; } = null!;
        public string? Industry { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? Website { get; set; }
        public string? ContactEmail { get; set; }
    }
}
