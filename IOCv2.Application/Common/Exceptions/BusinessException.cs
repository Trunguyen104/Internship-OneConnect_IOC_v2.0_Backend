using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Common.Exceptions
{
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message)
        {
        }

        public List<string> Errors { get; set; } = new();

        public BusinessException(string message, List<string> errors) : base(message)
        {
            Errors = errors;
        }
    }
}
