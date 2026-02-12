using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Services
{
    internal class UserServices : IUserServices
    {
        public readonly IUnitOfWork _unitOfWork;

        public UserServices(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateUserCodeAsync(UserRole role)
        {
            string prefix = role switch
            {
                UserRole.SuperAdmin => "SU",
                UserRole.SchoolAdmin => "SC",
                UserRole.EnterpriseAdmin => "EN",
                UserRole.HR => "HR",
                UserRole.Mentor => "ME",
                UserRole.Student => "ST",
                _ => "US"
            };

            var repo = _unitOfWork.Repository<UserCodeSequence>();

            var sequence = await repo.Query()
                .FirstOrDefaultAsync(x => x.Role == role);

            if (sequence == null)
            {
                sequence = new UserCodeSequence
                {
                    Role = role,
                    CurrentNumber = 1
                };
                await repo.AddAsync(sequence);
            }
            else
            {
                sequence.CurrentNumber++;
                await repo.UpdateAsync(sequence);
            }

            await _unitOfWork.SaveChangeAsync();

            return $"{prefix}{sequence.CurrentNumber:D6}";
        }

    }
}
