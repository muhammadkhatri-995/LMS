using LMS.API.Enums;

namespace LMS.API.Common;

public static class CourseOwnership
{
    // Only the owner passes. Someone who may see the course gets "forbidden".
    // Someone who may not (a draft that is not theirs) is told it does not exist.
    public static (bool Allowed, ServiceErrorType ErrorType, string Message) Check(CourseAccess? access, Guid userId)
    {
        if (access is null)
        {
            return (false, ServiceErrorType.NotFound, "Course not found.");
        }

        if (access.InstructorId == userId)
        {
            return (true, ServiceErrorType.None, string.Empty);
        }

        return access.Status == CourseStatus.published
            ? (false, ServiceErrorType.Forbidden, "You do not have permission to change this course.")
            : (false, ServiceErrorType.NotFound, "Course not found.");
    }
}