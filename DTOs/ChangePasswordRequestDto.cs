using System.ComponentModel.DataAnnotations;
namespace LMS.API.DTOs
{
    public class ChangePasswordRequestDto
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(72, MinimumLength = 8, ErrorMessage = "Password must be 8 to 72 characters.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one letter and one number.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
