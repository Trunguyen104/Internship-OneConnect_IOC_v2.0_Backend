using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Application.Commons.Exceptions
{
    public class ApplicationException : Exception
    {
        public string ErrorCode { get; set; }
        public ApplicationException(string errorCode, string message) : base(message) {
            ErrorCode = errorCode;
        }
    }
}
