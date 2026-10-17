using LMS.API.DTOs;
using LMS.API.Models;

namespace LMS.API.Common;

public static class CurriculumMapper
{
    public static SectionResponseDto ToResponse(this Section section)
    {
        return new SectionResponseDto
        {
            Id = section.Id,
            CourseId = section.CourseId,
            Title = section.Title,
            Position = section.Position
        };
    }

    public static LectureResponseDto ToResponse(this Lecture lecture)
    {
        return new LectureResponseDto
        {
            Id = lecture.Id,
            SectionId = lecture.SectionId,
            Title = lecture.Title,
            Description = lecture.Description,
            DurationMinutes = lecture.DurationMinutes,
            Position = lecture.Position,
            IsPublished = lecture.IsPublished,
            HasVideo = !string.IsNullOrEmpty(lecture.VideoPath),
            CreatedAt = lecture.CreatedAt,
            UpdatedAt = lecture.UpdatedAt
        };
    }
}