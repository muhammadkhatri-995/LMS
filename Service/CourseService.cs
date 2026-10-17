using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Enums;
using LMS.API.Interfaces;
using LMS.API.Models;

namespace LMS.API.Services;

public class CourseService : ICourseService
{
    private const string CourseNotFoundMessage = "Course not found.";

    private readonly ICourseRepository _courses;
    private readonly ICategoryRepository _categories;
    private readonly ICurriculumRepository _curriculum;
    private readonly IStorageService _storage;

    public CourseService(
        ICourseRepository courses,
        ICategoryRepository categories,
        ICurriculumRepository curriculum,
        IStorageService storage)
    {
        _courses = courses;
        _categories = categories;
        _curriculum = curriculum;
        _storage = storage;
    }

    // ---------- Reading ----------

    public async Task<ServiceResult<PagedResult<CourseResponseDto>>> GetPublishedAsync(
        string? search, Guid? categoryId, int page, int pageSize)
    {
        var filter = BuildFilter(search, categoryId, null, CourseStatus.published, page, pageSize);
        return await QueryAsync(filter);
    }

    public async Task<ServiceResult<PagedResult<CourseResponseDto>>> GetMineAsync(
        Guid instructorId, string? search, CourseStatus? status, int page, int pageSize)
    {
        var filter = BuildFilter(search, null, instructorId, status, page, pageSize);
        return await QueryAsync(filter);
    }

    public async Task<ServiceResult<PagedResult<CourseResponseDto>>> GetAllAsync(
        string? search, CourseStatus? status, Guid? categoryId, int page, int pageSize)
    {
        var filter = BuildFilter(search, categoryId, null, status, page, pageSize);
        return await QueryAsync(filter);
    }

    public async Task<ServiceResult<CourseResponseDto>> GetByIdAsync(Guid courseId, Guid userId, bool isAdmin)
    {
        var course = await _courses.GetDetailsAsync(courseId);

        if (course is null)
        {
            return ServiceResult<CourseResponseDto>.Fail(CourseNotFoundMessage, ServiceErrorType.NotFound);
        }

        var canSee = course.Status == CourseStatus.published
                     || isAdmin
                     || course.InstructorId == userId;

        if (!canSee)
        {
            // A draft belongs to its owner. Everyone else is told it does not exist.
            return ServiceResult<CourseResponseDto>.Fail(CourseNotFoundMessage, ServiceErrorType.NotFound);
        }

        ResolveThumbnailUrl(course);

        return ServiceResult<CourseResponseDto>.Ok(course);
    }

    // ---------- Writing ----------

    public async Task<ServiceResult<CourseResponseDto>> CreateAsync(Guid instructorId, SaveCourseRequestDto request)
    {
        var title = request.Title.Trim();
        var description = request.Description.Trim();

        var error = await ValidateAsync(title, description, request.CategoryId);

        if (error is not null)
        {
            return ServiceResult<CourseResponseDto>.Fail(error, ServiceErrorType.Validation);
        }

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            CategoryId = request.CategoryId!.Value,
            InstructorId = instructorId,
            Status = CourseStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        await _courses.AddAsync(course);

        return await ReloadAsync(course.Id);
    }

    public async Task<ServiceResult<CourseResponseDto>> UpdateAsync(
        Guid courseId, Guid userId, SaveCourseRequestDto request)
    {
        var course = await _courses.GetByIdAsync(courseId);

        if (course is null)
        {
            return ServiceResult<CourseResponseDto>.Fail(CourseNotFoundMessage, ServiceErrorType.NotFound);
        }

        // Only the owner may edit. Admins moderate but do not rewrite someone's course.
        if (course.InstructorId != userId)
        {
            return Denied<CourseResponseDto>(course);
        }

        var title = request.Title.Trim();
        var description = request.Description.Trim();

        var error = await ValidateAsync(title, description, request.CategoryId);

        if (error is not null)
        {
            return ServiceResult<CourseResponseDto>.Fail(error, ServiceErrorType.Validation);
        }

        course.Title = title;
        course.Description = description;
        course.CategoryId = request.CategoryId!.Value;
        course.UpdatedAt = DateTime.UtcNow;

        await _courses.UpdateAsync(course);

        return await ReloadAsync(course.Id);
    }

    public async Task<ServiceResult<CourseResponseDto>> SetPublishedAsync(
        Guid courseId, Guid userId, bool isAdmin, bool publish)
    {
        var course = await _courses.GetByIdAsync(courseId);

        if (course is null)
        {
            return ServiceResult<CourseResponseDto>.Fail(CourseNotFoundMessage, ServiceErrorType.NotFound);
        }

        if (!isAdmin && course.InstructorId != userId)
        {
            return Denied<CourseResponseDto>(course);
        }

        // A course needs at least one published lecture to go live.
        if (publish && course.Status != CourseStatus.published)
        {
            if (await _curriculum.CountPublishedLecturesAsync(course.Id) == 0)
            {
                return ServiceResult<CourseResponseDto>.Fail(
                    "Add and publish at least one lecture before publishing the course.",
                    ServiceErrorType.Conflict);
            }
        }

        var wanted = publish ? CourseStatus.published : CourseStatus.Draft;

        if (course.Status != wanted)
        {
            course.Status = wanted;
            course.PublishedAt = publish ? DateTime.UtcNow : null;
            course.UpdatedAt = DateTime.UtcNow;

            await _courses.UpdateAsync(course);
        }

        return await ReloadAsync(course.Id);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid courseId, Guid userId, bool isAdmin)
    {
        var course = await _courses.GetByIdAsync(courseId);

        if (course is null)
        {
            return ServiceResult<bool>.Fail(CourseNotFoundMessage, ServiceErrorType.NotFound);
        }

        if (!isAdmin && course.InstructorId != userId)
        {
            return Denied<bool>(course);
        }

        // Remember which files belong to this course before the rows disappear.
        var videoKeys = await _curriculum.GetVideoKeysForCourseAsync(course.Id);
        var thumbnailKey = course.ThumbnailPath;

        // Sections and lectures are deleted by the database (ON DELETE CASCADE).
        await _courses.DeleteAsync(course);

        // Only after the database delete succeeded do we delete the files.
        foreach (var key in videoKeys)
        {
            await _storage.DeleteAsync(StorageCategory.Video, key);
        }

        if (!string.IsNullOrEmpty(thumbnailKey))
        {
            await _storage.DeleteAsync(StorageCategory.Thumbnail, thumbnailKey);
        }

        return ServiceResult<bool>.Ok(true);
    }

    // ---------- Helpers ----------

    private async Task<string?> ValidateAsync(string title, string description, Guid? categoryId)
    {
        if (title.Length < 3)
        {
            return "Title must be at least 3 characters.";
        }

        if (description.Length < 10)
        {
            return "Description must be at least 10 characters.";
        }

        if (categoryId is null || await _categories.GetByIdAsync(categoryId.Value) is null)
        {
            return "Please choose a valid category.";
        }

        return null;
    }

    // Someone who may see the course gets "forbidden". Someone who may not is told it doesn't exist.
    private static ServiceResult<T> Denied<T>(Course course)
    {
        return course.Status == CourseStatus.published
            ? ServiceResult<T>.Fail("You do not have permission to change this course.", ServiceErrorType.Forbidden)
            : ServiceResult<T>.Fail(CourseNotFoundMessage, ServiceErrorType.NotFound);
    }

    // The repository puts the stored file key in ThumbnailUrl. The storage service turns it into
    // a real address (or null). Swapping local disk for S3 later changes only the storage service.
    private void ResolveThumbnailUrl(CourseResponseDto course)
    {
        course.ThumbnailUrl = _storage.GetPublicUrl(StorageCategory.Thumbnail, course.ThumbnailUrl);
    }

    private async Task<ServiceResult<CourseResponseDto>> ReloadAsync(Guid id)
    {
        var course = await _courses.GetDetailsAsync(id);

        if (course is null)
        {
            return ServiceResult<CourseResponseDto>.Fail(CourseNotFoundMessage, ServiceErrorType.NotFound);
        }

        ResolveThumbnailUrl(course);

        return ServiceResult<CourseResponseDto>.Ok(course);
    }

    private static CourseFilter BuildFilter(
        string? search, Guid? categoryId, Guid? instructorId, CourseStatus? status, int page, int pageSize)
    {
        return new CourseFilter
        {
            Search = search,
            CategoryId = categoryId,
            InstructorId = instructorId,
            Status = status,
            Page = Math.Max(page, 1),
            PageSize = Math.Clamp(pageSize, 1, 50)
        };
    }

    private async Task<ServiceResult<PagedResult<CourseResponseDto>>> QueryAsync(CourseFilter filter)
    {
        var (items, totalCount) = await _courses.GetPagedAsync(filter);

        foreach (var item in items)
        {
            ResolveThumbnailUrl(item);
        }

        var result = new PagedResult<CourseResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };

        return ServiceResult<PagedResult<CourseResponseDto>>.Ok(result);
    }
}