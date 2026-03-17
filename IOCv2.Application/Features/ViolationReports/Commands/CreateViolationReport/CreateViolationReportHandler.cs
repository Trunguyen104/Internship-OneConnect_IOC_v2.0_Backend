using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.ViolationReport;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Commands.CreateViolationReport
{
    public class CreateViolationReportHandler : IRequestHandler<CreateViolationReportCommand, Result<CreateViolationReportResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateViolationReportHandler> _logger;
        private readonly IMapper _mapper;

        public CreateViolationReportHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService, ILogger<CreateViolationReportHandler> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Result<CreateViolationReportResponse>> Handle(CreateViolationReportCommand request, CancellationToken cancellationToken)
        {
            _unitOfWork.BeginTransactionAsync().Wait();
            try {
                var currentUserId = Guid.Parse(_currentUserService.UserId!);
                // Kiểm tra xem Mentor chỉ chọn được SV thuộc nhóm mình
                if (ViolationReportParam.MentorRole.Equals(_currentUserService.Role)) {
                    var mentorInternshipGroupId = await _unitOfWork.Repository<InternshipGroup>().Query().Where(x => x.Mentor!.UserId == currentUserId).Select(x => x.InternshipId).FirstOrDefaultAsync(cancellationToken);
                    if (mentorInternshipGroupId == Guid.Empty)
                    {
                        return Result<CreateViolationReportResponse>.Failure(
                            "Bạn không quản lý nhóm nào.", ResultErrorType.NotFound);
                    }
                    // Lấy thông tin student
                    var student = await _unitOfWork.Repository<Student>()
                        .GetByIdAsync(request.StudentId, cancellationToken);

                    if (student == null)
                    {
                        return Result<CreateViolationReportResponse>.Failure(
                            "Sinh viên không tồn tại.", ResultErrorType.NotFound);
                    }

                    bool isInMentorGroup = await _unitOfWork.Repository<InternshipStudent>().Query().AnyAsync(x=>x.InternshipId == mentorInternshipGroupId && x.StudentId == student.StudentId, cancellationToken);
                    if (!isInMentorGroup) return Result<CreateViolationReportResponse>.Failure("Sinh viên này không thuộc nhóm bạn quản lý.", ResultErrorType.Forbidden);
                }
                // - Ngày xảy ra (bắt buộc). Ngày xảy ra **không được là ngày tương lai.** Ngày xảy ra **không được trước ngày bắt đầu kỳ thực tập** của sinh viên đó
                var studentTerm = await _unitOfWork.Repository<StudentTerm>().Query().FirstOrDefaultAsync(x=>x.StudentId == request.StudentId,cancellationToken);
                DateOnly termStart = await _unitOfWork.Repository<Term>().Query().Where(x => x.TermId == studentTerm!.TermId).Select(x=>x.StartDate).FirstOrDefaultAsync(cancellationToken);
                if (request.OccurredDate > DateTime.UtcNow.Date) return Result<CreateViolationReportResponse>.Failure("Ngày xảy ra vi phạm không được là ngày trong tương lai.");
                if (request.OccurredDate < (DateTime)termStart) 
                // Tạo report và lưu vào database


            }
            catch (Exception ex) { }
        }

    }


}
