using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup
{
    public class DeleteInternshipGroupHandler : IRequestHandler<DeleteInternshipGroupCommand, Result<DeleteInternshipGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<DeleteInternshipGroupHandler> _logger;
        private readonly ICacheService _cacheService;

        public DeleteInternshipGroupHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            IMapper mapper,
            ILogger<DeleteInternshipGroupHandler> logger,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<DeleteInternshipGroupResponse>> Handle(DeleteInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleting), request.InternshipId);

            try
            {
                // ── 1. Load nhóm kèm tất cả navigation cần thiết để phán xét ──────
                var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .Include(g => g.Members)
                    .Include(g => g.Logbooks)
                    .Include(g => g.ViolationReports)
                    .Include(g => g.Projects)
                        .ThenInclude(p => p.WorkItems)
                    .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

                if (entity == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNotFound), request.InternshipId);
                    return Result<DeleteInternshipGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
                }

                // ── 2. Chỉ nhóm Active mới được phép xóa (AC-G09) ────────────────
                if (entity.Status != GroupStatus.Active)
                {
                    _logger.LogWarning("Attempted to delete group {InternshipId} with status {Status}.", request.InternshipId, entity.Status);
                    return Result<DeleteInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.GroupNotActive),
                        ResultErrorType.BadRequest);
                }

                // ── 3. Kiểm tra "data thực tế" (AC-G09) ──────────────────────────
                // Data thực tế = logbook entries + vi phạm + project có WorkItem
                // KHÔNG tính: SV trong nhóm, project chưa có nội dung
                var hasActivityData = HasRealActivityData(entity);

                if (hasActivityData)
                {
                    _logger.LogWarning(
                        "Cannot delete group {InternshipId}: group has actual activity data (logbooks={L}, violations={V}, projectsWithItems={P}).",
                        request.InternshipId,
                        entity.Logbooks.Count,
                        entity.ViolationReports.Count,
                        entity.Projects.Count(p => p.WorkItems.Any()));

                    return Result<DeleteInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.HasActivityData),
                        ResultErrorType.BadRequest);
                }

                // ── 4. Không có data thực tế → tiến hành xóa ─────────────────────
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // a) Auto-unlink tất cả SV (InternshipStudent records)
                //    EF cascade sẽ tự xóa các bản ghi InternshipStudent khi nhóm bị xóa,
                //    nhưng log rõ để trace.
                if (entity.Members.Any())
                {
                    _logger.LogInformation(
                        "Auto-unlinking {Count} student(s) from group {InternshipId} before deletion.",
                        entity.Members.Count, request.InternshipId);
                    // EF cascade delete sẽ xử lý, trạng thái Placed của StudentTerm KHÔNG thay đổi theo AC
                }

                // b) Unlink projects không có nội dung: project sẽ bị cascade-deleted cùng nhóm.
                //    (Project.InternshipId là required FK nên không thể null ra; chúng sẽ bị xóa.)
                if (entity.Projects.Any(p => !p.WorkItems.Any()))
                {
                    _logger.LogInformation(
                        "Group {InternshipId} has {Count} project(s) without content — will be removed with the group.",
                        request.InternshipId,
                        entity.Projects.Count(p => !p.WorkItems.Any()));
                }

                // c) Hard delete nhóm — cascade xóa Members, Projects (không có content), Stakeholders, etc.
                await _unitOfWork.Repository<InternshipGroup>().DeleteAsync(entity);
                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved > 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(request.InternshipId), cancellationToken);
                    await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);
                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogDeletedSuccess), request.InternshipId);

                    var response = _mapper.Map<DeleteInternshipGroupResponse>(entity);
                    return Result<DeleteInternshipGroupResponse>.Success(response);
                }

                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(_messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleteFailed));
                return Result<DeleteInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleteError), request.InternshipId);
                return Result<DeleteInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }

        /// <summary>
        /// Kiểm tra xem nhóm có "data thực tế" hay không.
        /// Data thực tế = logbook entries, vi phạm, hoặc ít nhất 1 project đã có WorkItems.
        /// KHÔNG tính: SV trong nhóm, project chỉ được link nhưng chưa có nội dung.
        /// </summary>
        private static bool HasRealActivityData(InternshipGroup group)
        {
            // 1. Có bất kỳ logbook entry nào
            if (group.Logbooks.Any())
                return true;

            // 2. Có bất kỳ báo cáo vi phạm nào
            if (group.ViolationReports.Any())
                return true;

            // 3. Có bất kỳ project nào đã có WorkItems (task/submission/logbook)
            if (group.Projects.Any(p => p.WorkItems.Any()))
                return true;

            return false;
        }
    }
}
