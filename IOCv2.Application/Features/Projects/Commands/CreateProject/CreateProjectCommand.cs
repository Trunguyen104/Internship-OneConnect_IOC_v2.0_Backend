using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace IOCv2.Application.Features.Projects.Commands.CreateProject
{
    /// <summary>DTO cho link tài liệu đính kèm khi tạo project</summary>
    public class CreateProjectLinkDto
    {
        /// <summary>Tên hiển thị cho link</summary>
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>URL tài liệu (http/https)</summary>
        public string Url { get; set; } = string.Empty;
    }

    public class CreateProjectCommand : IRequest<Result<CreateProjectResponse>>
    {
        public Guid? InternshipId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Project details
        public string Field { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public string? Deliverables { get; set; }
        public ProjectTemplate Template { get; set; } = ProjectTemplate.None;

        /// <summary>Tùy chọn — nếu null BE sẽ tự generate theo slugify algorithm.</summary>
        public string? ProjectCode { get; set; }

        /// <summary>
        /// Upload file tài liệu đính kèm (pdf, docx, xlsx, pptx, zip, rar, jpg, png — tối đa theo giới hạn từng loại).
        /// Gửi qua multipart/form-data.
        /// </summary>
        public List<IFormFile>? Files { get; set; }

        /// <summary>
        /// Link tài liệu đính kèm (tuỳ chọn, có thể cung cấp cùng lúc với file).
        /// </summary>
        public List<CreateProjectLinkDto>? Links { get; set; }
    }
}
