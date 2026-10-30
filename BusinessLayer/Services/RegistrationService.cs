using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Data;
using EntityLayer.Models;

namespace BusinessLayer.Services
{
    public class RegistrationService
    {
        private readonly CourseDbContext _context;

        public RegistrationService(CourseDbContext context)
        {
            _context = context;
        }

        public class ServiceResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        public async Task<ServiceResult> EnrollAsync(string learnerId, int courseId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                    return new ServiceResult { Success = false, Message = "Course not found." };

                // Removed .Include() since CompletedPrerequisites is handled via value converter natively
                var learner = await _context.Learners.FirstOrDefaultAsync(l => l.Id == learnerId);
                if (learner == null)
                    return new ServiceResult { Success = false, Message = "Learner not found." };

                bool isEnrolled = await _context.Enrollments.AnyAsync(e => e.LearnerId == learnerId && e.CourseId == courseId);
                bool isWaitlisted = await _context.Waitlists.AnyAsync(w => w.LearnerId == learnerId && w.CourseId == courseId);

                if (isEnrolled)
                    return new ServiceResult { Success = true, Message = "Learner is already enrolled in this course." };
                if (isWaitlisted)
                    return new ServiceResult { Success = true, Message = "Learner is already on the waitlist for this course." };

                bool hasPrerequisite = string.IsNullOrEmpty(course.RequiredPrerequisite) ||
                                       learner.CompletedPrerequisites.Contains(course.RequiredPrerequisite);

                if (!hasPrerequisite)
                    return new ServiceResult { Success = false, Message = $"Prerequisite '{course.RequiredPrerequisite}' not met." };

                int currentEnrollments = await _context.Enrollments.CountAsync(e => e.CourseId == courseId);

                if (currentEnrollments < course.Capacity)
                {
                    _context.Enrollments.Add(new Enrollment { LearnerId = learnerId, CourseId = courseId, EnrolledAt = DateTime.UtcNow });
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return new ServiceResult { Success = true, Message = "Successfully enrolled in the course." };
                }
                else
                {
                    _context.Waitlists.Add(new Waitlist { LearnerId = learnerId, CourseId = courseId, JoinedAt = DateTime.UtcNow });
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return new ServiceResult { Success = true, Message = "Course is full. Learner added to the waitlist." };
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ServiceResult { Success = false, Message = $"An error occurred during enrollment: {ex.Message}" };
            }
        }

        public async Task<ServiceResult> DropAsync(string learnerId, int courseId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.LearnerId == learnerId && e.CourseId == courseId);

                if (enrollment == null)
                {
                    var waitlistEntry = await _context.Waitlists
                        .FirstOrDefaultAsync(w => w.LearnerId == learnerId && w.CourseId == courseId);

                    if (waitlistEntry != null)
                    {
                        _context.Waitlists.Remove(waitlistEntry);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return new ServiceResult { Success = true, Message = "Learner removed from waitlist." };
                    }

                    return new ServiceResult { Success = false, Message = "Active enrollment or waitlist entry not found." };
                }

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();

                var waitlistQueue = await _context.Waitlists
                    .Where(w => w.CourseId == courseId)
                    .OrderBy(w => w.JoinedAt)
                    .ToListAsync();

                foreach (var entry in waitlistQueue)
                {
                    var waitlistedLearner = await _context.Learners
                        .FirstOrDefaultAsync(l => l.Id == entry.LearnerId);

                    var course = await _context.Courses.FindAsync(courseId);

                    if (waitlistedLearner != null && course != null)
                    {
                        bool isEligible = string.IsNullOrEmpty(course.RequiredPrerequisite) ||
                                          waitlistedLearner.CompletedPrerequisites.Contains(course.RequiredPrerequisite);

                        if (isEligible)
                        {
                            _context.Enrollments.Add(new Enrollment
                            {
                                LearnerId = entry.LearnerId,
                                CourseId = courseId,
                                EnrolledAt = DateTime.UtcNow
                            });

                            _context.Waitlists.Remove(entry);
                            await _context.SaveChangesAsync();
                            break;
                        }
                    }
                }

                await transaction.CommitAsync();
                return new ServiceResult { Success = true, Message = "Learner dropped successfully. Waitlist updated." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ServiceResult { Success = false, Message = $"An error occurred during drop: {ex.Message}" };
            }
        }
    }
}