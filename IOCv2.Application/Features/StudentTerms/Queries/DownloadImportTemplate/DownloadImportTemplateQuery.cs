using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Queries.DownloadImportTemplate;

public record DownloadImportTemplateQuery : IRequest<Result<DownloadImportTemplateResponse>>;
