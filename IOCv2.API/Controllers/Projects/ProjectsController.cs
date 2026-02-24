using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Projects.Queries.GetProjectById;
using IOCv2.Application.Features.Projects.Queries.GetProjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Projects;

/// <summary>
/// Quản lý thông tin Dự án thực tập
/// </summary>
[Tags("Projects")]
[Authorize]
public class ProjectsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách các dự án (có hỗ trợ phân trang, tìm kiếm và lọc theo trạng thái)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Result<PaginatedResult<GetProjectsResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjects([FromQuery] GetProjectsQuery query)
    {
        var result = await _mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Lấy chi tiết thông tin một dự án bao gồm danh sách thành viên
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<GetProjectByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectDetail(Guid id)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery { ProjectId = id });
        return HandleResult(result);
    }
}
