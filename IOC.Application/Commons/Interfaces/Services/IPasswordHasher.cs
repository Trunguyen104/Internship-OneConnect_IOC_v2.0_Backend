using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Application.Commons.Interfaces.Services
{
    public interface IPasswordHasher
    {
        // Hash plain password for storage
        string Hash(string password);

        // Verify provided plain password against stored hash
        bool Verify(string hashedPassword, string providedPassword);
    }

}
