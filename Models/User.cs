using LMS.API.Enums;
namespace LMS.API.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public String FullName { get; set; } = String.Empty;
        public String Email { get; set; } = String.Empty;
        public String PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }= DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

    }
}
