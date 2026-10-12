using System.ComponentModel.DataAnnotations;

namespace LMS.API.DTOs;

public class SaveCategoryRequestDto
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2 to 100 characters.")]
    public string Name { get; set; } = string.Empty;
}