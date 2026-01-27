using IOC.Domain.Enums;
using System;

namespace IOC.Application.Commons.Interfaces.Services
{
    // Exposes current authenticated user information (derived from JWT claims)
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        AdminRole Role { get; }
        Guid? OrganizationId { get; }
    }
}