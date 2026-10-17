namespace LMS.API.Models;

public class Lecture
{
    public Guid Id { get; set; }

    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int Position { get; set; }
    public bool IsPublished { get; set; }

    // Filled in Module 4, when video upload exists.
    public string? VideoPath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}