using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityLayer.Models
{
    public class Enrollment
    {
        public int Id { get; set; }
        public required string LearnerId { get; set; }
        public int CourseId { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Learner? Learner { get; set; }
        public Course? Course { get; set; }
    }
}
