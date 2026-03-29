using System;

namespace IOCv2.Application.Common.Exceptions
{
    public class ConflictException : Exception
    {
        public string? PropertyName { get; }

        public ConflictException(string message, string? propertyName = null) : base(message)
        {
            PropertyName = propertyName;
        }

        public ConflictException(string entityName, object key, string? propertyName = null)
            : base($"Entity \"{entityName}\" with key ({key}) already exists.")
        {
            PropertyName = propertyName;
        }
    }
}
