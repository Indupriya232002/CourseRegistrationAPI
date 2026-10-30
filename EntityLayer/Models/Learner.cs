using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityLayer.Models
{
    public class Learner
    {
        public required string Id { get; set; } // e.g., "alice"
        public required string Name { get; set; }
        public ICollection<string> CompletedPrerequisites { get; set; } = new List<string>();
    }
}
