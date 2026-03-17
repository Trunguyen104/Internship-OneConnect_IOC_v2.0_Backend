namespace IOCv2.Application.Features.StudentTerms.Queries.DownloadImportTemplate;

public record DownloadImportTemplateResponse
{
    public byte[] FileContent { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = "import_students_template.xlsx";
    public string ContentType { get; init; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
