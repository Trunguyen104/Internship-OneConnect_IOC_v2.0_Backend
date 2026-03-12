using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Enterprises.Commands.CreateEnterprise;
using IOCv2.Application.Features.Enterprises.Commands.DeleteEnterprise;
using IOCv2.Application.Features.Enterprises.Commands.RestoreEnterprise;
using IOCv2.Application.Features.Enterprises.Commands.UpdateEnterprise;
using IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseByHR;
using IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseById;
using IOCv2.Application.Features.Enterprises.Queries.GetEnterprises;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IOCv2.API.Controllers.Enterprises;

[Authorize]
[Tags("Enterprises")]
public class EnterprisesController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EnterprisesController> _logger;

    public EnterprisesController(IMediator mediator, ILogger<EnterprisesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of enterprises based on filter criteria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetEnterprisesResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnterprises(
        [FromQuery] GetEnterprisesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves an enterprise by its unique identifier.
    /// </summary>
    [HttpGet("{enterpriseId:guid}", Name = "GetEnterpriseById")]
    [ProducesResponseType(typeof(ApiResponse<GetEnterpriseByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnterpriseById(
        [FromRoute] Guid enterpriseId,
        CancellationToken cancellationToken)
    {
        var query = new GetEnterpriseByIdQuery { Id = enterpriseId };
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates a new enterprise.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<CreateEnterpriseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEnterprise(
        [FromBody] CreateEnterpriseCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetEnterpriseById), new { enterpriseId = result.Data?.EnterpriseId, version = "1" });
    }

    /// <summary>
    /// Updates an existing enterprise.
    /// </summary>
    [HttpPut("{enterpriseId:guid}")]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<UpdateEnterpriseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEnterprise([FromRoute] Guid enterpriseId, [FromBody] UpdateEnterpriseCommand command, CancellationToken cancellationToken)
    {
        var updateCommand = command with { EnterpriseId = enterpriseId };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Deletes an enterprise by its unique identifier.
    /// </summary>
    [HttpDelete("{enterpriseId:guid}")]
    [Authorize(Roles = "EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DeleteEnterpriseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEnterprise([FromRoute] Guid enterpriseId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteEnterpriseCommand { EnterpriseId = enterpriseId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves the enterprise associated with the currently authenticated HR user.
    /// </summary>
    [HttpGet("HR")]
    [Authorize(Roles = "HR")]
    [ProducesResponseType(typeof(ApiResponse<GetEnterpriseByHRResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnterpriseByHR(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEnterpriseByHRCommand(), cancellationToken);
        return HandleResult(result);
    }

}
