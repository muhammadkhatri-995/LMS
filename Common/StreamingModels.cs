using LMS.API.Enums;

namespace LMS.API.Common;

// What the streaming rules need to know about a lecture.
public record LectureStreamInfo(
    Guid LectureId,
    string? VideoPath,
    bool IsPublished,
    Guid InstructorId,
    CourseStatus CourseStatus);

// What the streaming rules need to know about the viewer.
public record UserStatus(bool IsActive, UserRole Role);

// An opened video file and its content type. The caller disposes the stream.
public sealed record VideoSource(Stream Content, string ContentType);