using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.PublicHolidays.Commands.DeletePublicHoliday;

public class DeletePublicHolidayHandler
    : IRequestHandler<DeletePublicHolidayCommand, Result<DeletePublicHolidayResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<DeletePublicHolidayHandler> _logger;

    public DeletePublicHolidayHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<DeletePublicHolidayHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<DeletePublicHolidayResponse>> Handle(
        DeletePublicHolidayCommand request, CancellationToken cancellationToken)
    {
        var holiday = await _unitOfWork.Repository<PublicHoliday>()
            .Query()
            .FirstOrDefaultAsync(h => h.PublicHolidayId == request.PublicHolidayId, cancellationToken);

        if (holiday is null)
        {
            _logger.LogWarning("Public holiday {Id} not found.", request.PublicHolidayId);
            return Result<DeletePublicHolidayResponse>.Failure(
                _messageService.GetMessage(MessageKeys.PublicHolidays.NotFound),
                ResultErrorType.NotFound);
        }

        await _unitOfWork.Repository<PublicHoliday>().DeleteAsync(holiday, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        _logger.LogInformation("Deleted public holiday {Id} on {Date}.", holiday.PublicHolidayId, holiday.Date);

        return Result<DeletePublicHolidayResponse>.Success(
            new DeletePublicHolidayResponse
            {
                PublicHolidayId = holiday.PublicHolidayId,
                Date            = holiday.Date
            },
            _messageService.GetMessage(MessageKeys.PublicHolidays.DeleteSuccess));
    }
}
