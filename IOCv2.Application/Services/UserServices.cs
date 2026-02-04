using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IOCv2.Application.Constants.MessageKeys;

namespace IOCv2.Application.Services
{
    internal class UserServices : IUserServices
    {
        public readonly IUnitOfWork _unitOfWork;

        public UserServices(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        //public async Task<string> GenerateUserCodeAsync(UserRole role)
        //{
        //    //char prefix = role switch
        //    //{
        //    //    UserRole.SuperAdmin => 'SA',
        //    //    UserRole.Mentor => 'C',
        //    //    UserRole.SchoolAdmin => 'W',
        //    //    UserRole.Student => 'B',
        //    //    _ => 'U'
        //    //};

        //    //var currentCount = await _unitOfWork.Repository<Employee>()
        //    //    .Query()
        //    //    .CountAsync(e => e.Role == role);
        //    //int nextNumber = currentCount + 1;

        //    //// Format "D6" sẽ biến số 1 thành "000001"
        //    //return $"{prefix}{nextNumber.ToString("D6")}";
        //}
    }
}
