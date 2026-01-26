using IOC.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Domain.ValueObjects
{
    public sealed class Email : ValueObject
    {
        public string Value { get; }

        private Email(string value)
        {
            Value = value;
        }

        public static Email Create(string value)
        {
                if (string.IsNullOrWhiteSpace(value))
                    throw new DomainException("Email cannot be empty");

                if (!IsValid(value))
                    throw new DomainException("Invalid email");

                return new Email(value.Trim().ToLower());
        }

        private static bool IsValid(string email)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(
                email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
            );
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
