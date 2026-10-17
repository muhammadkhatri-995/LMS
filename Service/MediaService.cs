using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Enums;
using LMS.API.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LMS.API.Services;

public class MediaService : IMediaService
{
    private const string CourseNotFoundMessage = "Course not found.";
    private const string LectureNotFoundMessage = "Lecture not found.";

    private readonly ICourseRepository _courses;
    private readonly ICurriculumRepository _curriculum;
    private readonly IStorageService _storage;

    public MediaService(ICourseRepository courses, ICurriculumRepository curriculum, IStorageService storage)
    {
        _courses = courses;
        _curriculum = curriculum;
        _storage = storage;
    }

    // ---------- Thumbnail ----------

    public async Task<ServiceResult<ThumbnailResponseDto>> UploadThumbnailAsync(
        Guid courseId, Guid userId, IFormFile file)
    {
        var course = await _courses.GetByIdAsync(courseId);

        if (course is null)
        {
            return ServiceResult<ThumbnailResponseDto>.Fail(CourseNotFoundMessage, ServiceErrorType.NotFound);
        }

        var (allowed, errorType, message) = CourseOwnership.Check(
            new CourseAccess(course.Id, course.InstructorId, course.Status), userId);

        if (!allowed)
        {
            return ServiceResult<ThumbnailResponseDto>.Fail(message, errorType);
        }

        var (extension, error) = UploadRules.Validate(file, UploadKind.Thumbnail);

        if (error is not null)
        {
            return ServiceResult<ThumbnailResponseDto>.Fail(error, ServiceErrorType.Validation);
        }

        // 1) Save the new file. 2) Point the database at it. 3) Delete the old file.
        await using var stream = file.OpenReadStream();
        var stored = await _storage.SaveAsync(StorageCategory.Thumbnail, stream, extension!);

        var oldKey = course.ThumbnailPath;
        course.ThumbnailPath = stored.Key;
        course.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _courses.UpdateAsync(course);
        }
        catch
        {
            // The database did not change, so the new file would be an orphan.
            await _storage.DeleteAsync(StorageCategory.Thumbnail, stored.Key);
            throw;
        }

        if (!string.IsNullOrEmpty(oldKey))
        {
            await _storage.DeleteAsync(StorageCategory.Thumbnail, oldKey);
        }

        return ServiceResult<ThumbnailResponseDto>.Ok(new ThumbnailResponseDto
        {
            ThumbnailUrl = _storage.GetPublicUrl(StorageCategory.Thumbnail, stored.Key)
        });
    }

    public async Task<ServiceResult<bool>> RemoveThumbnailAsync(Guid courseId, Guid userId)
    {
        var course = await _courses.GetByIdAsync(courseId);

        if (course is null)
        {
            return ServiceResult<bool>.Fail(CourseNotFoundMessage, ServiceErrorType.NotFound);
        }

        var (allowed, errorType, message) = CourseOwnership.Check(
            new CourseAccess(course.Id, course.InstructorId, course.Status), userId);

        if (!allowed)
        {
            return ServiceResult<bool>.Fail(message, errorType);
        }

        var oldKey = course.ThumbnailPath;

        if (string.IsNullOrEmpty(oldKey))
        {
            return ServiceResult<bool>.Ok(true);
        }

        course.ThumbnailPath = null;
        course.UpdatedAt = DateTime.UtcNow;
        await _courses.UpdateAsync(course);

        await _storage.DeleteAsync(StorageCategory.Thumbnail, oldKey);

        return ServiceResult<bool>.Ok(true);
    }

    // ---------- Lecture video ----------

    public async Task<ServiceResult<LectureResponseDto>> UploadVideoAsync(
        Guid lectureId, Guid userId, IFormFile file)
    {
        var lecture = await _curriculum.GetLectureAsync(lectureId);

        if (lecture is null)
        {
            return ServiceResult<LectureResponseDto>.Fail(LectureNotFoundMessage, ServiceErrorType.NotFound);
        }

        var access = await _curriculum.GetCourseAccessAsync(lecture.Section.CourseId);
        var (allowed, errorType, message) = CourseOwnership.Check(access, userId);

        if (!allowed)
        {
            return ServiceResult<LectureResponseDto>.Fail(message, errorType);
        }

        var (extension, error) = UploadRules.Validate(file, UploadKind.Video);

        if (error is not null)
        {
            return ServiceResult<LectureResponseDto>.Fail(error, ServiceErrorType.Validation);
        }

        await using var stream = file.OpenReadStream();
        var stored = await _storage.SaveAsync(StorageCategory.Video, stream, extension!);

        var oldKey = lecture.VideoPath;
        lecture.VideoPath = stored.Key;
        lecture.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _curriculum.SaveChangesAsync();
        }
        catch
        {
            await _storage.DeleteAsync(StorageCategory.Video, stored.Key);
            throw;
        }

        if (!string.IsNullOrEmpty(oldKey))
        {
            await _storage.DeleteAsync(StorageCategory.Video, oldKey);
        }

        return ServiceResult<LectureResponseDto>.Ok(lecture.ToResponse());
    }

    public async Task<ServiceResult<bool>> RemoveVideoAsync(Guid lectureId, Guid userId)
    {
        var lecture = await _curriculum.GetLectureAsync(lectureId);

        if (lecture is null)
        {
            return ServiceResult<bool>.Fail(LectureNotFoundMessage, ServiceErrorType.NotFound);
        }

        var access = await _curriculum.GetCourseAccessAsync(lecture.Section.CourseId);
        var (allowed, errorType, message) = CourseOwnership.Check(access, userId);

        if (!allowed)
        {
            return ServiceResult<bool>.Fail(message, errorType);
        }

        var oldKey = lecture.VideoPath;

        if (string.IsNullOrEmpty(oldKey))
        {
            return ServiceResult<bool>.Ok(true);
        }

        lecture.VideoPath = null;
        lecture.UpdatedAt = DateTime.UtcNow;
        await _curriculum.SaveChangesAsync();

        await _storage.DeleteAsync(StorageCategory.Video, oldKey);

        return ServiceResult<bool>.Ok(true);
    }
}