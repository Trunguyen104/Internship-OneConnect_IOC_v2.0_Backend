using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Stakeholders.DTOs;
using MediatR;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholderById
{
    public record GetStakeholderByIdQuery : IRequest<Result<StakeholderDto>>
    {
        public Guid Id { get; init; }
    }
}
