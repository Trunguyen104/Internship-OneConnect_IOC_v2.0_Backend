using IOCv2.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Infrastructure.Security
{
    public class PasswordService : IPasswordService
    {
        private static readonly char[] AlphanumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        private static readonly char[] SpecialChars = "!@#$%^&*()_-+=".ToCharArray();

        public string GenerateRandomPassword()
        {
            // Policy: 8 alphanumeric + 1 special character
            int alphaLength = 8;
            int specialLength = 1;
            char[] password = new char[alphaLength + specialLength];
            int index = 0;

            // Generate 8 alphanumeric characters
            for (int i = 0; i < alphaLength; i++)
            {
                password[index++] = AlphanumericChars[RandomNumberGenerator.GetInt32(AlphanumericChars.Length)];
            }

            // Generate 1 special character
            for (int i = 0; i < specialLength; i++)
            {
                password[index++] = SpecialChars[RandomNumberGenerator.GetInt32(SpecialChars.Length)];
            }

            // Shuffle the result using Fisher-Yates algorithm to mix the special char
            int n = password.Length;
            while (n > 1)
            {
                n--;
                int k = RandomNumberGenerator.GetInt32(n + 1);
                var value = password[k];
                password[k] = password[n];
                password[n] = value;
            }

            return new string(password);
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}
