using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.UniAssign.Commands.BulkUnassign
{
    public record BulkUnassignCommand : IRequest<Result<BulkUnassignResponse>>
    {
        /// <summary>
        /// Danh sách studentIds được chọn trên giao diện
        /// </summary>
        public List<Guid> StudentIds { get; set; } = new();

    }
}