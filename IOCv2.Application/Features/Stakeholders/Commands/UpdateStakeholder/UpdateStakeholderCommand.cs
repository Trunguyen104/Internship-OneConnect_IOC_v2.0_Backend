﻿using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder
{
    /// <summary>
    /// Command to update an existing stakeholder.
    /// </summary>
    public record UpdateStakeholderCommand : IRequest<Result<UpdateStakeholderResponse>>
    {
        /// <summary>
        /// The ID of the stakeholder to update.
        /// </summary>
        [JsonIgnore]
        public Guid StakeholderId { get; init; }

        /// <summary>
        /// The ID of the internship group.
        /// </summary>
        public Guid InternshipId { get; init; }

        /// <summary>
        /// The new name of the stakeholder (optional).
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// The new type of the stakeholder (Real, Persona) (optional).
        /// </summary>
        public StakeholderType? Type { get; init; }

        /// <summary>
        /// The new role of the stakeholder (optional).
        /// </summary>
        public string? Role { get; init; }

        /// <summary>
        /// The new description of the stakeholder (optional).
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// The new email address of the stakeholder (optional).
        /// </summary>
        public string? Email { get; init; }

        /// <summary>
        /// The new phone number of the stakeholder (optional).
        /// </summary>
        public string? PhoneNumber { get; init; }
    }
}
