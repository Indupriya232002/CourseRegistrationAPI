using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityLayer.Models
{
    public class Course
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public int Capacity { get; set; }
        public required string RequiredPrerequisite { get; set; }
    }
}
