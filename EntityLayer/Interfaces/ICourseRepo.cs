using EntityLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityLayer.Interfaces
{
    public interface ICourseRepo
    {
        Learner? GetLearnerById(string learnerId);
        Course? GetCourseDetails(int courseId);
    }
}
