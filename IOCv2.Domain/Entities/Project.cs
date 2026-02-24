﻿using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Project : BaseEntity
{
    public int InternshipId { get; set; }
    public int? MentorId { get; set; }
    public string ProjectName { get; set; } = null!;
    public string? Description { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    
    // Navigation properties
    public virtual ICollection<Stakeholder> Stakeholders { get; set; } = new List<Stakeholder>();
}

