using System.ComponentModel.DataAnnotations;

namespace LMS.API.DTOs;

public class SaveSectionRequestDto
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be 2 to 200 characters.")]
    public string Title { get; set; } = string.Empty;
}