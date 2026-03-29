using IOCv2.Application.Common.Models;
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
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Project details
        public string Field { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public string? Deliverables { get; set; }

        /// <summary>F1 (AC-02): Gán ngay Intern Group khi tạo (optional)</summary>
        public Guid? InternshipGroupId { get; set; }

        /// <summary>F1 (AC-02): true = Mentor nhấn Save → Published; false = auto-save → Draft</summary>
        public bool PublishOnSave { get; set; } = false;

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
