using System.ComponentModel.DataAnnotations;

namespace LMS.API.DTOs;

public class UpdateProfileRequestDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be 2 to 100 characters.")]
    public string FullName { get; set; } = string.Empty;
}