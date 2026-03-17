using ClosedXML.Excel;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Queries.DownloadImportTemplate;

public class DownloadImportTemplateHandler
    : IRequestHandler<DownloadImportTemplateQuery, Result<DownloadImportTemplateResponse>>
{
    public Task<Result<DownloadImportTemplateResponse>> Handle(
        DownloadImportTemplateQuery request, CancellationToken cancellationToken)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Danh sách sinh viên");

        // Headers (must match the expected import template exactly)
        ws.Cell(1, 1).Value = "Mã sinh viên";
        ws.Cell(1, 2).Value = "Họ và tên";
        ws.Cell(1, 3).Value = "Email";
        ws.Cell(1, 4).Value = "Số điện thoại";
        ws.Cell(1, 5).Value = "Ngày sinh";

        // Header style
        var headerRange = ws.Range(1, 1, 1, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
        headerRange.Style.Font.FontColor = XLColor.White;

        // Sample rows
        ws.Cell(2, 1).Value = "SV001";
        ws.Cell(2, 2).Value = "Nguyễn Văn An";
        ws.Cell(2, 3).Value = "nguyenvanan@example.com";
        ws.Cell(2, 4).Value = "0901234567";
        ws.Cell(2, 5).Value = "01/01/2002";

        ws.Cell(3, 1).Value = "SV002";
        ws.Cell(3, 2).Value = "Trần Thị Bình";
        ws.Cell(3, 3).Value = "tranthibinh@example.com";
        ws.Cell(3, 4).Value = "0912345678";
        ws.Cell(3, 5).Value = "15/06/2001";

        // Auto-fit columns
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        return Task.FromResult(Result<DownloadImportTemplateResponse>.Success(
            new DownloadImportTemplateResponse { FileContent = ms.ToArray() }));
    }
}
