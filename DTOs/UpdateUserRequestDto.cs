using LMS.API.Enums;
namespace LMS.API.DTOs
{
    public class UpdateUserRequestDto
    {
        public String FullName { get; set; } = string.Empty;
        public String Email { get; set; } = string.Empty;
        public UserRole Role { get; set; } 

    }
}
