using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Uploads.Commands.UploadImage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IOCv2.API.Controllers.Uploads;

[Tags("Upload Management")]
[Authorize]
public class UploadsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UploadsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Upload an image file to a specific folder.
    /// </summary>
    /// <param name="command">File and Folder name</param>
    /// <returns>Public URL of the uploaded image</returns>
    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageCommand command)
    {
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }
}
