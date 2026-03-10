using IOCv2.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Common.Helpers
{
    public static class CurrentUserHelper
    {
        public static Guid GetValidGuidUserId(string currentUserId)
        {
            try {
                return Guid.Parse(currentUserId);
            } catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
