using System.ComponentModel.DataAnnotations;

namespace LMS.API.DTOs;

public class SaveLectureRequestDto
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be 2 to 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description must be at most 2000 characters.")]
    public string? Description { get; set; }

    [Range(0, 600, ErrorMessage = "Duration must be between 0 and 600 minutes.")]
    public int DurationMinutes { get; set; }
}