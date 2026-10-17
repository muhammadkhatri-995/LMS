namespace LMS.API.DTOs;

public class CurriculumResponseDto
{
    public Guid CourseId { get; set; }
    public List<SectionResponseDto> Sections { get; set; } = new();
    public int TotalLectures { get; set; }
    public int TotalDurationMinutes { get; set; }
}