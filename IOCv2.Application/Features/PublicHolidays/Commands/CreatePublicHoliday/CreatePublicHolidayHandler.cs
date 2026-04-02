using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.PublicHolidays.Commands.CreatePublicHoliday;

public class CreatePublicHolidayHandler
    : IRequestHandler<CreatePublicHolidayCommand, Result<CreatePublicHolidayResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<CreatePublicHolidayHandler> _logger;

    public CreatePublicHolidayHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<CreatePublicHolidayHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<CreatePublicHolidayResponse>> Handle(
        CreatePublicHolidayCommand request, CancellationToken cancellationToken)
    {
        // Duplicate date check
        var exists = await _unitOfWork.Repository<PublicHoliday>()
            .ExistsAsync(h => h.Date == request.Date, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Public holiday on {Date} already exists.", request.Date);
            return Result<CreatePublicHolidayResponse>.Failure(
                _messageService.GetMessage(MessageKeys.PublicHolidays.AlreadyExists),
                ResultErrorType.Conflict);
        }

        var holiday = PublicHoliday.Create(request.Date, request.Description);

        await _unitOfWork.Repository<PublicHoliday>().AddAsync(holiday, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        _logger.LogInformation("Created public holiday {Id} on {Date}.", holiday.PublicHolidayId, holiday.Date);

        return Result<CreatePublicHolidayResponse>.Success(
            new CreatePublicHolidayResponse
            {
                PublicHolidayId = holiday.PublicHolidayId,
                Date            = holiday.Date,
                Description     = holiday.Description
            },
            _messageService.GetMessage(MessageKeys.PublicHolidays.CreateSuccess));
    }
}
