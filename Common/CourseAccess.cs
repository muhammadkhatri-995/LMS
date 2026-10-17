using LMS.API.Enums;

namespace LMS.API.Common;

public record CourseAccess(Guid Id, Guid InstructorId, CourseStatus Status);