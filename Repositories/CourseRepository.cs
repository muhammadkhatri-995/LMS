using System.Linq.Expressions;
using LMS.API.Common;
using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Interfaces;
using LMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly AppDbContext _db;

    public CourseRepository(AppDbContext db)
    {
        _db = db;
    }

    // Becomes a SQL SELECT of only these columns (no password hash is ever read).
    private static readonly Expression<Func<Course, CourseResponseDto>> ToResponse = c => new CourseResponseDto
    {
        Id = c.Id,
        Title = c.Title,
        Description = c.Description,
        CategoryId = c.CategoryId,
        CategoryName = c.Category.Name,
        InstructorId = c.InstructorId,
        InstructorName = c.Instructor.FullName,
        Status = c.Status,
        ThumbnailUrl = c.ThumbnailPath,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        PublishedAt = c.PublishedAt
    };

    public async Task<Course?> GetByIdAsync(Guid id)
    {
        return await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CourseResponseDto?> GetDetailsAsync(Guid id)
    {
        return await _db.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync();
    }

    public async Task<(List<CourseResponseDto> Items, int TotalCount)> GetPagedAsync(CourseFilter filter)
    {
        var query = _db.Courses.AsNoTracking().AsQueryable();

        if (filter.InstructorId.HasValue)
        {
            query = query.Where(c => c.InstructorId == filter.InstructorId.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId == filter.CategoryId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(c => c.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = SqlLike.ContainsPattern(filter.Search);

            query = query.Where(c =>
                EF.Functions.ILike(c.Title, pattern, "\\") ||
                EF.Functions.ILike(c.Description, pattern, "\\"));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(ToResponse)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Course course)
    {
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Course course)
    {
        _db.Courses.Update(course);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Course course)
    {
        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();
    }
}