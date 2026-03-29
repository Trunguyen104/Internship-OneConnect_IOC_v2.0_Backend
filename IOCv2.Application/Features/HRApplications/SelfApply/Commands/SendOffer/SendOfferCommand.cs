using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Commands.SendOffer;

public record SendOfferCommand(Guid ApplicationId) : IRequest<Result<SendOfferResponse>>;
