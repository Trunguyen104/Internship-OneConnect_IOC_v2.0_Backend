﻿using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholderById
{
    /// <summary>
    /// Query to get a single stakeholder by ID.
    /// </summary>
    public record GetStakeholderByIdQuery : IRequest<Result<GetStakeholderByIdResponse>>
    {
        /// <summary>
        /// The ID of the stakeholder to retrieve.
        /// </summary>
        public Guid Id { get; init; }
    }
}
