using System.Text.RegularExpressions;
using ClosedXML.Excel;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudentsPreview;

public class ImportStudentsPreviewHandler : IRequestHandler<ImportStudentsPreviewCommand, Result<ImportStudentsPreviewResponse>>
{
    private static readonly Regex StudentCodeRegex = new(@"^[a-zA-Z0-9\-_\.]+$", RegexOptions.Compiled);
    private static readonly Regex FullNameRegex = new(@"^[\p{L}\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^(\+84|0)[0-9]{9,10}$", RegexOptions.Compiled);
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    private const int MaxRows = 1000;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly string[] _requiredHeaders;

    public ImportStudentsPreviewHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;

        _requiredHeaders = new[]
        {
            messageService.GetMessage(MessageKeys.StudentTerms.ExcelHeaderStudentCode),
            messageService.GetMessage(MessageKeys.StudentTerms.ExcelHeaderFullName),
            messageService.GetMessage(MessageKeys.StudentTerms.ExcelHeaderEmail),
            messageService.GetMessage(MessageKeys.StudentTerms.ExcelHeaderPhone),
            messageService.GetMessage(MessageKeys.StudentTerms.ExcelHeaderDateOfBirth),
        };
    }

    public async Task<Result<ImportStudentsPreviewResponse>> Handle(ImportStudentsPreviewCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);
        var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        // Validate term
        var term = await _unitOfWork.Repository<Term>()
            .Query()
            .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

        if (term == null)
            return Result<ImportStudentsPreviewResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Terms.NotFound), ResultErrorType.NotFound);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (term.Status != TermStatus.Open || term.EndDate < today)
            return Result<ImportStudentsPreviewResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.TermNotOpen));

        if (!isSuperAdmin)
        {
            var universityUser = await _unitOfWork.Repository<UniversityUser>()
                .Query()
                .FirstOrDefaultAsync(uu => uu.UserId == userId, cancellationToken);

            if (universityUser == null || universityUser.UniversityId != term.UniversityId)
                return Result<ImportStudentsPreviewResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
        }

        // Validate file
        var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
            return Result<ImportStudentsPreviewResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.InvalidFileFormat));

        if (request.File.Length > MaxFileSizeBytes)
            return Result<ImportStudentsPreviewResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.FileTooLarge));

        // Parse Excel
        using var stream = request.File.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        // Validate headers
        var headers = Enumerable.Range(1, _requiredHeaders.Length)
            .Select(col => worksheet.Cell(1, col).GetValue<string>().Trim())
            .ToArray();

        for (int i = 0; i < _requiredHeaders.Length; i++)
        {
            if (!string.Equals(headers[i], _requiredHeaders[i], StringComparison.OrdinalIgnoreCase))
                return Result<ImportStudentsPreviewResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.InvalidExcelHeaders));
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var dataRows = lastRow - 1;

        if (dataRows > MaxRows)
            return Result<ImportStudentsPreviewResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.TooManyRows));

        // Load existing enrollments in this term (code + email)
        var existingInTerm = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Where(st => st.TermId == request.TermId)
            .Select(st => new { Code = st.Student.User.UserCode, Email = st.Student.User.Email })
            .ToListAsync(cancellationToken);

        var existingCodesInTerm = existingInTerm.Select(x => x.Code.ToLower()).ToHashSet();
        var existingEmailsInTerm = existingInTerm.Select(x => x.Email.ToLower()).ToHashSet();

        // Load enrollments in other active terms (cross-term check)
        var existingInActiveTerms = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Where(st => st.TermId != request.TermId && st.EnrollmentStatus == EnrollmentStatus.Active)
            .Select(st => new { Code = st.Student.User.UserCode, Email = st.Student.User.Email })
            .ToListAsync(cancellationToken);

        var crossTermCodes = existingInActiveTerms.Select(x => x.Code.ToLower()).ToHashSet();
        var crossTermEmails = existingInActiveTerms.Select(x => x.Email.ToLower()).ToHashSet();

        // Process rows
        var previewRows = new List<ImportPreviewRow>();
        var codesInFile = new HashSet<string>();
        var emailsInFile = new HashSet<string>();

        for (int row = 2; row <= lastRow; row++)
        {
            var studentCode = worksheet.Cell(row, 1).GetValue<string>().Trim();
            var fullName = worksheet.Cell(row, 2).GetValue<string>().Trim();
            var email = worksheet.Cell(row, 3).GetValue<string>().Trim();
            var phone = worksheet.Cell(row, 4).GetValue<string>().Trim();
            var dob = worksheet.Cell(row, 5).GetValue<string>().Trim();
            var major = worksheet.Cell(row, 6).GetValue<string>().Trim();

            var errors = new List<string>();

            // Validate StudentCode
            if (string.IsNullOrWhiteSpace(studentCode))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeRequired));
            else if (!StudentCodeRegex.IsMatch(studentCode))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeInvalidDetail));
            else if (codesInFile.Contains(studentCode.ToLower()))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeDuplicateInFile));
            else if (existingCodesInTerm.Contains(studentCode.ToLower()))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeAlreadyInTerm));
            else if (crossTermCodes.Contains(studentCode.ToLower()))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeInOtherTerm));

            // Validate FullName
            if (string.IsNullOrWhiteSpace(fullName))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.FullNameRequired));
            else if (!FullNameRegex.IsMatch(fullName))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.FullNameInvalid));

            // Validate Email
            if (string.IsNullOrWhiteSpace(email))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.EmailRequired));
            else if (!IsValidEmail(email))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.EmailInvalid));
            else if (emailsInFile.Contains(email.ToLower()))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.EmailDuplicateInFile));
            else if (existingEmailsInTerm.Contains(email.ToLower()))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.EmailAlreadyInTerm));
            else if (crossTermEmails.Contains(email.ToLower()))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.EmailInOtherTerm));

            // Validate Phone (optional)
            if (!string.IsNullOrWhiteSpace(phone) && !PhoneRegex.IsMatch(phone))
                errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.PhoneInvalid));

            // Validate DOB (optional)
            if (!string.IsNullOrWhiteSpace(dob))
            {
                if (!TryParseDob(dob, out var dobDate))
                    errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.DateOfBirthInvalidFormat));
                else if (!IsAtLeast15(dobDate))
                    errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.DateOfBirthMinAge));
            }

            if (!string.IsNullOrWhiteSpace(studentCode))
                codesInFile.Add(studentCode.ToLower());
            if (!string.IsNullOrWhiteSpace(email))
                emailsInFile.Add(email.ToLower());

            previewRows.Add(new ImportPreviewRow
            {
                RowNumber = row,
                StudentCode = studentCode,
                FullName = fullName,
                Email = email,
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                DateOfBirth = string.IsNullOrWhiteSpace(dob) ? null : dob,
                Major = string.IsNullOrWhiteSpace(major) ? null : major,
                IsValid = errors.Count == 0,
                Errors = errors
            });
        }

        var response = new ImportStudentsPreviewResponse
        {
            TotalRows = previewRows.Count,
            ValidRows = previewRows.Count(r => r.IsValid),
            InvalidRows = previewRows.Count(r => !r.IsValid),
            PreviewData = previewRows
        };

        return Result<ImportStudentsPreviewResponse>.Success(response,
            _messageService.GetMessage(MessageKeys.StudentTerms.ImportPreviewSuccess));
    }

    private static bool IsValidEmail(string email)
    {
        try { var addr = new System.Net.Mail.MailAddress(email); return addr.Address == email; }
        catch { return false; }
    }

    private static bool TryParseDob(string value, out DateOnly result)
    {
        return DateOnly.TryParseExact(value, "dd/MM/yyyy", out result);
    }

    private static bool IsAtLeast15(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return today.Year - dob.Year >= 15;
    }
}
