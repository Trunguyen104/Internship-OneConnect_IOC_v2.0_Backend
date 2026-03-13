using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Queries.GetViolationReports
{
    public record GetViolationReportsQuery : IRequest<Result<PaginatedResult<GetViolationReportsResponse>>>
    {
        // Search
        public string? SearchTerm { get; set; } // Tên sinh viên hoặc MSSV

        // Filters
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ViolationType? ViolationType { get; set; }        // Loại vi phạm
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ViolationSeverity? SeverityLevel { get; set; }        // Mức độ
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ViolationStatus? ProcessingStatus { get; set; }     // Trạng thái xử lý
        public Guid? CreatedById { get; set; }            // Người tạo
        public DateOnly? OccurredFrom { get; set; }       // Khoảng thời gian - ngày bắt đầu
        public DateOnly? OccurredTo { get; set; }         // Khoảng thời gian - ngày kết thúc
        public Guid? GroupId { get; set; }                // Nhóm

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
