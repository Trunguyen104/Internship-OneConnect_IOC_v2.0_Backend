using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Public.SendReservationEmail
{
    public class SendReservationEmailCommand : IRequest<Result<bool>>
    {
        public string PartnerType { get; set; } = "University";
        public string PartnerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string HiringCount { get; set; } = string.Empty;
        public string ConsultationDate { get; set; } = string.Empty;
        public string SelectedTime { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
