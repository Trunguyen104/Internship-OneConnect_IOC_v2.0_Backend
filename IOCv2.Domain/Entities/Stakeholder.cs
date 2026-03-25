﻿using IOCv2.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace IOCv2.Domain.Entities
{
    public class Stakeholder : BaseEntity
    {
        [Key]
        public Guid Id { get; private set; }
        public Guid InternshipId { get; private set; }
        public string Name { get; private set; } = null!;
        public StakeholderType Type { get; private set; } = StakeholderType.Real;
        public string? Role { get; private set; }
        public string? Description { get; private set; }
        public string Email { get; private set; } = null!;
        public string? PhoneNumber { get; private set; }

        public virtual InternshipGroup InternshipGroup { get; private set; } = null!;
        public virtual ICollection<StakeholderIssue> Issues { get; private set; } = new List<StakeholderIssue>();

        /// <summary>
        /// Constructor for EF Core persistence.
        /// </summary>
        private Stakeholder() { }

        /// <summary>
        /// Initializes a new instance of the Stakeholder entity.
        /// </summary>
        public Stakeholder(
            Guid internshipId,
            string name,
            StakeholderType type,
            string email,
            string? role = null,
            string? description = null,
            string? phoneNumber = null)
        {
            Id = Guid.NewGuid();
            InternshipId = internshipId;
            Name = name;
            Type = type;
            Email = email;
            Role = role;
            Description = description;
            PhoneNumber = phoneNumber;
        }

        /// <summary>
        /// Updates stakeholder details.
        /// </summary>
        public void UpdateDetails(
            string name,
            StakeholderType type,
            string email,
            string? role,
            string? description,
            string? phoneNumber)
        {
            Name = name;
            Type = type;
            Email = email;
            Role = role;
            Description = description;
            PhoneNumber = phoneNumber;
        }
    }
}
