using LMS.API.Enums;

namespace LMS.API.Common;

public class CourseFilter
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? InstructorId { get; set; }
    public CourseStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
