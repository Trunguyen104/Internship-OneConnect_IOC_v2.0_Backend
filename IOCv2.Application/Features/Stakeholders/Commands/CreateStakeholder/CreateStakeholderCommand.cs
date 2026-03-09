using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder
{
    /// <summary>
    /// Command to create a new stakeholder.
    /// </summary>
    public record CreateStakeholderCommand : IRequest<Result<CreateStakeholderResponse>>
    {
        /// <summary>
        /// The ID of the internship group the stakeholder belongs to.
        /// </summary>
        public Guid InternshipId { get; init; }

        /// <summary>
        /// The name of the stakeholder.
        /// </summary>
        public string Name { get; init; } = null!;

        /// <summary>
        /// The type of the stakeholder (Real, Persona).
        /// </summary>
        public StakeholderType Type { get; init; } = StakeholderType.Real;

        /// <summary>
        /// The role of the stakeholder in the project.
        /// </summary>
        public string? Role { get; init; }

        /// <summary>
        /// A brief description of the stakeholder.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// The email address of the stakeholder.
        /// </summary>
        public string Email { get; init; } = null!;

        /// <summary>
        /// The phone number of the stakeholder.
        /// </summary>
        public string? PhoneNumber { get; init; }
    }
}
