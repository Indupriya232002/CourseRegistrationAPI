using DataAccessLayer.Data;
using EntityLayer.Interfaces;
using EntityLayer.Models;

namespace DataAccessLayer.Repositories
{
    public class CourseRepo(CourseDbContext context) : ICourseRepo
    {
        private readonly CourseDbContext _context = context;

        public Learner? GetLearnerById(string learnerId)
        {
            return _context.Learners.Find(learnerId);
        }

        public Course? GetCourseDetails(int courseId)
        {
            return _context.Courses.Find(courseId);
        }
    }
}