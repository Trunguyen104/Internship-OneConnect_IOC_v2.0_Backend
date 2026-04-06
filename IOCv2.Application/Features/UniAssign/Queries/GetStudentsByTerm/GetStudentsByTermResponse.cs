using IOCv2.Application.Features.UniAssign.Queries.GetStudentsByTerm.GetStudentsByTermDTOs;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.UniAssign.Queries.GetStudentsByTerm
{
    public record GetStudentsByTermResponse
    {
        public Guid TermId { get; init; }
        public string TermName { get; init; } = string.Empty;
        public List<StudentDto> Students { get; init; } = new List<StudentDto>();
    }
}
