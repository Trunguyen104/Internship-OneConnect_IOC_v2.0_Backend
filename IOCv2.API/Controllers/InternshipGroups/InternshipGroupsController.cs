using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup;
using IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroupById;
using IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.InternshipGroups
{
    [Tags("Internship Groups Management")]
    [Authorize]
    public class InternshipGroupsController : ApiControllerBase
    {
        private readonly ISender _mediator;

        public InternshipGroupsController(ISender mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lấy danh sách nhóm thực tập (hỗ trợ phân trang, tìm kiếm, lọc)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<PaginatedResult<GetInternshipGroupsResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInternshipGroups([FromQuery] GetInternshipGroupsQuery query)
        {
            return HandleResult(await _mediator.Send(query));
        }

        /// <summary>
        /// Xem chi tiết thông tin một nhóm thực tập
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<GetInternshipGroupByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInternshipGroupById(Guid id)
        {
            return HandleResult(await _mediator.Send(new GetInternshipGroupByIdQuery(id)));
        }

        /// <summary>
        /// Tạo mới nhóm thực tập
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<CreateInternshipGroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateInternshipGroup([FromBody] CreateInternshipGroupCommand command)
        {
            return HandleResult(await _mediator.Send(command));
        }

        /// <summary>
        /// Cập nhật thông tin nhóm thực tập
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Result<UpdateInternshipGroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateInternshipGroup(Guid id, [FromBody] UpdateInternshipGroupCommand command)
        {
            var updateCommand = command with { InternshipId = id };
            return HandleResult(await _mediator.Send(updateCommand));
        }

        /// <summary>
        /// Xóa bỏ nhóm thực tập và danh sách sinh viên đi kèm
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result<DeleteInternshipGroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteInternshipGroup(Guid id)
        {
            return HandleResult(await _mediator.Send(new DeleteInternshipGroupCommand(id)));
        }

        /// <summary>
        /// Bổ sung danh sách sinh viên vào nhóm
        /// </summary>
        [HttpPost("{id}/students")]
        [ProducesResponseType(typeof(Result<AddStudentsToGroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddStudentsToGroup(Guid id, [FromBody] AddStudentsToGroupCommand command)
        {
            var updateCommand = command with { InternshipId = id };
            return HandleResult(await _mediator.Send(updateCommand));
        }

        /// <summary>
        /// Gỡ bỏ sinh viên khỏi nhóm hiện tại
        /// </summary>
        [HttpDelete("{id}/students")]
        [ProducesResponseType(typeof(Result<RemoveStudentsFromGroupResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveStudentsFromGroup(Guid id, [FromBody] RemoveStudentsFromGroupCommand command)
        {
            var updateCommand = command with { InternshipId = id };
            return HandleResult(await _mediator.Send(updateCommand));
        }
    }
}
