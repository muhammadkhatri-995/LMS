using System.ComponentModel.DataAnnotations;
using LMS.API.Enums;
namespace LMS.API.DTOs
{
    public class CreateUserRequestDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be 2 to 100 characters.")]
        public string FullName { get; set; } = string.Empty;



        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email format is invalid.")]
        [StringLength(255, ErrorMessage = "Email must be at most 255 characters.")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Password is required.")]
        [StringLength(72, MinimumLength = 8, ErrorMessage = "Password must be 8 to 72 characters.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one letter and one number.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        [EnumDataType(typeof(UserRole), ErrorMessage = "Role must be Admin, Instructor, or Student.")]
        public UserRole? Role { get; set; }

    }
}
