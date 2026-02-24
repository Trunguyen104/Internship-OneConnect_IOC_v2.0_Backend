﻿using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholderById
{
    public record GetStakeholderByIdQuery : IRequest<Result<GetStakeholderByIdResponse>>
    {
        public Guid Id { get; init; }
    }
}
