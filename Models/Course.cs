using LMS.API.Enums;

namespace LMS.API.Models
{
    public class Course
    {
        public Guid Id { get; set; }
        public String Title { get; set; } = String.Empty;
        public String Description   { get; set; } = String.Empty;

        public Guid CategoryId { get; set; }
        public Category Category { get; set; }

        public Guid InstructorId { get; set; }
        public User Instructor { get; set; } = null;

        public CourseStatus Status { get; set; } = CourseStatus.Draft;

        public String? ThumbnailPath { get; set; } = String.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
