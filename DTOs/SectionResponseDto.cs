namespace LMS.API.DTOs;

public class SectionResponseDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public List<LectureResponseDto> Lectures { get; set; } = new();
}