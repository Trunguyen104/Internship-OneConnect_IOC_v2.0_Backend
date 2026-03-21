using ClosedXML.Excel;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.StudentTerms.Queries.DownloadImportTemplate;

public class DownloadImportTemplateHandler : IRequestHandler<DownloadImportTemplateQuery, Result<DownloadImportTemplateResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;

    public DownloadImportTemplateHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
    }

    public async Task<Result<DownloadImportTemplateResponse>> Handle(DownloadImportTemplateQuery request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);
        var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        var term = await _unitOfWork.Repository<Term>()
            .Query()
            .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

        if (term == null)
            return Result<DownloadImportTemplateResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Terms.NotFound), ResultErrorType.NotFound);

        if (!isSuperAdmin)
        {
            var universityUser = await _unitOfWork.Repository<UniversityUser>()
                .Query()
                .FirstOrDefaultAsync(uu => uu.UserId == userId, cancellationToken);

            if (universityUser == null || universityUser.UniversityId != term.UniversityId)
                return Result<DownloadImportTemplateResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(
            _messageService.GetMessage(MessageKeys.StudentTerms.ExcelWorksheetStudentList));

        // Headers
        var headers = new[]
        {
            _messageService.GetMessage(MessageKeys.StudentTerms.ExcelHeaderStudentCode),
            _messageService.GetMessage(MessageKeys.StudentTerms.ExcelHeaderFullName),
            _messageService.GetMessage(MessageKeys.StudentTerms.ExcelHeaderEmail),
            _messageService.GetMessage(MessageKeys.StudentTerms.ExcelHeaderPhone),
            _messageService.GetMessage(MessageKeys.StudentTerms.ExcelHeaderDateOfBirth),
            _messageService.GetMessage(MessageKeys.StudentTerms.ExcelHeaderMajor),
        };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Sample data row 1
        worksheet.Cell(2, 1).Value = "SV001";
        worksheet.Cell(2, 2).Value = "Nguyễn Văn A";
        worksheet.Cell(2, 3).Value = "nguyenvana@example.com";
        worksheet.Cell(2, 4).Value = "0901234567";
        worksheet.Cell(2, 5).Value = "01/01/2002";
        worksheet.Cell(2, 6).Value = "Công nghệ thông tin";

        // Sample data row 2
        worksheet.Cell(3, 1).Value = "SV002";
        worksheet.Cell(3, 2).Value = "Trần Thị B";
        worksheet.Cell(3, 3).Value = "tranthib@example.com";
        worksheet.Cell(3, 4).Value = "0912345678";
        worksheet.Cell(3, 5).Value = "15/06/2002";
        worksheet.Cell(3, 6).Value = "Quản trị kinh doanh";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return Result<DownloadImportTemplateResponse>.Success(new DownloadImportTemplateResponse
        {
            FileContent = content,
            FileName = $"template_import_sinhvien_{DateTime.UtcNow:yyyyMMdd}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        });
    }
}
