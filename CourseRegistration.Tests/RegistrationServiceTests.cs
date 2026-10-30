using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLayer.Services;
using DataAccessLayer.Data;
using EntityLayer.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CourseRegistration.Tests
{
    public class RegistrationServiceTests
    {
        private CourseDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<CourseDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new CourseDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task EnrollAsync_ShouldEnroll_WhenCapacityAvailableAndPrereqMet()
        {
            var context = GetInMemoryDbContext();
            context.Courses.Add(new Course { Id = 1, Title = "Intro to C#", Capacity = 2, RequiredPrerequisite = "" });
            context.Learners.Add(new Learner { Id = "L1", Name = "Alice", CompletedPrerequisites = new List<string>() });
            await context.SaveChangesAsync();

            var service = new RegistrationService(context);
            var result = await service.EnrollAsync("L1", 1);

            Assert.True(result.Success);
            Assert.Equal("Successfully enrolled in the course.", result.Message);
            Assert.Single(context.Enrollments);
        }

        [Fact]
        public async Task EnrollAsync_ShouldFail_WhenPrerequisiteNotMet()
        {
            var context = GetInMemoryDbContext();
            context.Courses.Add(new Course { Id = 1, Title = "Advanced C#", Capacity = 5, RequiredPrerequisite = "CS101" });
            context.Learners.Add(new Learner { Id = "L1", Name = "Bob", CompletedPrerequisites = new List<string> { "CS100" } });
            await context.SaveChangesAsync();

            var service = new RegistrationService(context);
            var result = await service.EnrollAsync("L1", 1);

            Assert.False(result.Success);
            Assert.Contains("prerequisite", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(context.Enrollments);
        }

        [Fact]
        public async Task EnrollAsync_ShouldFail_WhenAlreadyEnrolledOrWaitlisted()
        {
            var context = GetInMemoryDbContext();
            context.Courses.Add(new Course { Id = 1, Title = "Database Systems", Capacity = 5, RequiredPrerequisite = "" });
            context.Learners.Add(new Learner { Id = "L1", Name = "Charlie", CompletedPrerequisites = new List<string>() });
            context.Enrollments.Add(new Enrollment { LearnerId = "L1", CourseId = 1, EnrolledAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new RegistrationService(context);
            var result = await service.EnrollAsync("L1", 1);

            Assert.False(result.Success);
            Assert.Contains("already enrolled", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task EnrollAsync_ShouldAddToWaitlist_WhenCourseIsFull()
        {
            var context = GetInMemoryDbContext();
            context.Courses.Add(new Course { Id = 1, Title = "Web Dev", Capacity = 1, RequiredPrerequisite = "" });
            context.Learners.Add(new Learner { Id = "L1", Name = "Learner 1", CompletedPrerequisites = new List<string>() });
            context.Learners.Add(new Learner { Id = "L2", Name = "Learner 2", CompletedPrerequisites = new List<string>() });

            context.Enrollments.Add(new Enrollment { LearnerId = "L1", CourseId = 1, EnrolledAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new RegistrationService(context);
            var result = await service.EnrollAsync("L2", 1);

            Assert.True(result.Success);
            Assert.Contains("waitlist", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Single(context.Waitlists);
        }

        [Fact]
        public async Task DropAsync_ShouldPromoteEligibleWaitlistedLearner_AndSkipIneligibleInPlace()
        {
            var context = GetInMemoryDbContext();
            context.Courses.Add(new Course { Id = 1, Title = "Algorithms", Capacity = 1, RequiredPrerequisite = "CS101" });

            // Enrolled student
            context.Learners.Add(new Learner { Id = "L1", Name = "Enrolled", CompletedPrerequisites = new List<string> { "CS101" } });
            context.Enrollments.Add(new Enrollment { LearnerId = "L1", CourseId = 1, EnrolledAt = DateTime.UtcNow });

            // Waitlisted student 1 (Ineligible - missing CS101)
            context.Learners.Add(new Learner { Id = "L2", Name = "Waitlist Ineligible", CompletedPrerequisites = new List<string>() });
            context.Waitlists.Add(new Waitlist { LearnerId = "L2", CourseId = 1, JoinedAt = DateTime.UtcNow.AddMinutes(-10) });

            // Waitlisted student 2 (Eligible - has CS101)
            context.Learners.Add(new Learner { Id = "L3", Name = "Waitlist Eligible", CompletedPrerequisites = new List<string> { "CS101" } });
            context.Waitlists.Add(new Waitlist { LearnerId = "L3", CourseId = 1, JoinedAt = DateTime.UtcNow.AddMinutes(-5) });

            await context.SaveChangesAsync();

            var service = new RegistrationService(context);
            var result = await service.DropAsync("L1", 1);

            Assert.True(result.Success);

            // Verify L3 was promoted to Enrollments
            Assert.Contains(context.Enrollments, e => e.LearnerId == "L3");

            // Verify L2 was skipped in place and remains on the Waitlist
            Assert.Contains(context.Waitlists, w => w.LearnerId == "L2");
            Assert.DoesNotContain(context.Waitlists, w => w.LearnerId == "L3");
        }
    }
}