using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace IOCv2.Application.Features.Stakeholders.Common
{
	public static class StakeholderAccessGuard
	{
		private static readonly string[] ElevatedRoles = { "SchoolAdmin", "SuperAdmin", "Moderator" };

		public static bool IsMentor(string? role)
		{
			return string.Equals(role, "Mentor", StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsElevatedRole(string? role)
		{
			return ElevatedRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
		}

		public static bool IsStudent(string? role)
		{
			return string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase);
		}

		public static bool CanManage(string? role)
		{
			return IsStudent(role) || IsElevatedRole(role);
		}

		public static bool CanView(string? role)
		{
			return CanManage(role) || IsMentor(role);
		}

		public static Result<T>? EnsureAuthenticated<T>(ICurrentUserService currentUserService, IMessageService messageService)
		{
			if (string.IsNullOrWhiteSpace(currentUserService.UserId) || !Guid.TryParse(currentUserService.UserId, out _))
			{
				return Result<T>.Failure(messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
			}

			return null;
		}

		public static Result<T>? EnsureManagePermission<T>(ICurrentUserService currentUserService, IMessageService messageService)
		{
			if (CanManage(currentUserService.Role))
			{
				return null;
			}

			return Result<T>.Failure(messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
		}

		public static Guid GetCurrentUserId(ICurrentUserService currentUserService)
		{
			return Guid.Parse(currentUserService.UserId!);
		}

		public static async Task<bool> HasInternshipAccessAsync(
			IUnitOfWork unitOfWork,
			Guid internshipId,
			Guid currentUserId,
			CancellationToken cancellationToken)
		{
			return await unitOfWork.Repository<InternshipGroup>()
				.Query()
				.AnyAsync(g => g.InternshipId == internshipId &&
					(
						(g.Mentor != null && g.Mentor.UserId == currentUserId) ||
						g.Members.Any(m => m.Student.UserId == currentUserId)
					), cancellationToken);
		}

		public static async Task<Result<T>?> EnsureInternshipAccessAsync<T>(
			IUnitOfWork unitOfWork,
			IMessageService messageService,
			ICurrentUserService currentUserService,
			Guid internshipId,
			CancellationToken cancellationToken)
		{
			if (IsElevatedRole(currentUserService.Role))
			{
				return null;
			}

			var currentUserId = GetCurrentUserId(currentUserService);
			var hasInternshipAccess = await HasInternshipAccessAsync(unitOfWork, internshipId, currentUserId, cancellationToken);
			if (!hasInternshipAccess)
			{
				return Result<T>.Failure(messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
			}

			return null;
		}
	}
}



