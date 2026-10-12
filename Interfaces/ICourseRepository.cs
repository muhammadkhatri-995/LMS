using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Models;

namespace LMS.API.Interfaces;

public interface ICourseRepository
{
    // The tracked entity, used when we are going to change or delete it.
    Task<Course?> GetByIdAsync(Guid id);

    // Read-only views that go straight into DTOs.
    Task<CourseResponseDto?> GetDetailsAsync(Guid id);
    Task<(List<CourseResponseDto> Items, int TotalCount)> GetPagedAsync(CourseFilter filter);

    Task AddAsync(Course course);
    Task UpdateAsync(Course course);
    Task DeleteAsync(Course course);
}