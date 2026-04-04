using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.UniAssign.Commands.BulkAssign
{
    internal class BulkReassignEnterpriseCommand : IRequest<Result<BulkReassignEnterpriseResponse>>
    {
        /// <summary>
        /// Term c?a k? ðang thao tác (dùng ð? validate term status)
        /// </summary>
        public Guid TermId { get; set; }

        /// <summary>
        /// Enterprise m?i s? reassign t?i
        /// </summary>
        public Guid NewEnterpriseId { get; set; }
        public Guid NewInternPhaseId { get; set; }

        /// <summary>
        /// Danh sách studentIds ðý?c ch?n trên giao di?n
        /// </summary>
        public List<Guid> StudentIds { get; set; } = new();
    }
}