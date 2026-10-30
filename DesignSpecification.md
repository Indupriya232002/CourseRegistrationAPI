# Design Specification: Course Registration API

## 1. Architecture Overview
The Course Registration API follows a clean, multi-layered architecture to ensure separation of concerns and maintainability:
- **API Layer (`CourseRegistrationAPI`)**: Exposes RESTful controllers (`RegistrationsController`) and configures middleware, SQLite connection strings, and database seeding.
- **Business Layer (`BusinessLayer`)**: Houses core business logic services (`RegistrationService`), handling transaction management, capacity rules, FIFO waitlists, and prerequisite verifications.
- **DataAccess Layer (`DataAccessLayer`)**: Implements Entity Framework Core (`CourseDbContext`), managing database configurations, relationships, and custom value converters for primitive collections.
- **Entity Layer (`EntityLayer`)**: Defines domain models (`Course`, `Learner`, `Enrollment`, `Waitlist`) and required data constraints.

---

## 2. Core Business Logic & Algorithms

### A. Direct Enrollment & Capacity Management
- When a registration request is received, the service checks if the learner is already enrolled or waitlisted.
- It compares the current active enrollment count against the course's `Capacity`.
- If space is available, the learner is immediately added to the `Enrollments` table.

### B. FIFO Waitlist Management
- If the course capacity is fully utilized, the learner is placed on the waitlist with a chronological timestamp (`JoinedAt = DateTime.UtcNow`).

### C. Skip-in-Place Waitlist Promotion Policy
- When an enrolled learner drops a course, a database transaction is initiated.
- The system queries the waitlist ordered strictly by `JoinedAt` (First-In, First-Out).
- It iterates through waitlisted candidates to evaluate prerequisite eligibility:
  - **Eligible**: Promoted immediately to `Enrollments`, removed from `Waitlists`, and the promotion loop terminates.
  - **Ineligible**: Skipped **in place** (retaining their original position and timestamp on the waitlist) while evaluation continues for the next candidate in line.

### D. Transaction Safety & Concurrency
- All multi-table write operations (such as drops and promotional queue shifts) are wrapped in EF Core database transactions (`BeginTransactionAsync` / `CommitAsync` / `RollbackAsync`) to prevent partial state corruption under concurrent requests.