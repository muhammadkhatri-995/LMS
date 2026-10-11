using System.ComponentModel.DataAnnotations;

namespace LMS.API.DTOs;

public class SetUserStatusRequestDto
{
    [Required(ErrorMessage = "IsActive is required.")]
    public bool? IsActive { get; set; }
}