using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class StudentProject
    {
        public Guid StudentId { get; private set; }
        public Guid ProjectId { get; private set; }

        public Student Student { get; private set; }
        public Project Project { get; private set; }
    }
}
