using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Enums;
using LMS.API.Interfaces;
using LMS.API.Models;

namespace LMS.API.Services;

public class CurriculumService : ICurriculumService
{
    private const string CourseNotFoundMessage = "Course not found.";
    private const string SectionNotFoundMessage = "Section not found.";
    private const string LectureNotFoundMessage = "Lecture not found.";
    private const string TitleTooShortMessage = "Title must be at least 2 characters.";
    private const string KeepOneLectureMessage =
        "A published course must keep at least one published lecture. Unpublish the course first.";

    private readonly ICurriculumRepository _curriculum;
    private readonly IStorageService _storage;

    public CurriculumService(ICurriculumRepository curriculum, IStorageService storage)
    {
        _curriculum = curriculum;
        _storage = storage;
    }

    // ---------- Reading ----------

    public async Task<ServiceResult<CurriculumResponseDto>> GetCurriculumAsync(Guid courseId, Guid userId, bool isAdmin)
    {
        var access = await _curriculum.GetCourseAccessAsync(courseId);

        if (access is null)
        {
            return ServiceResult<CurriculumResponseDto>.Fail(CourseNotFoundMessage, ServiceErrorType.NotFound);
        }

        var isOwner = access.InstructorId == userId;
        var canSee = access.Status == CourseStatus.published || isAdmin || isOwner;

        if (!canSee)
        {
            // A draft belongs to its owner. Everyone else is told it does not exist.
            return ServiceResult<CurriculumResponseDto>.Fail(CourseNotFoundMessage, ServiceErrorType.NotFound);
        }

        // Owners and admins see drafts. Everyone else sees published lectures only.
        var includeUnpublished = isOwner || isAdmin;
        var sections = await _curriculum.GetCurriculumAsync(courseId, includeUnpublished);
        var lectures = sections.SelectMany(s => s.Lectures).ToList();

        return ServiceResult<CurriculumResponseDto>.Ok(new CurriculumResponseDto
        {
            CourseId = courseId,
            Sections = sections,
            TotalLectures = lectures.Count,
            TotalDurationMinutes = lectures.Sum(l => l.DurationMinutes)
        });
    }

    // ---------- Sections ----------

    public async Task<ServiceResult<SectionResponseDto>> CreateSectionAsync(
        Guid courseId, Guid userId, SaveSectionRequestDto request)
    {
        var (access, errorType, message) = await AuthorizeOwnerAsync(courseId, userId);

        if (access is null)
        {
            return ServiceResult<SectionResponseDto>.Fail(message, errorType);
        }

        var title = request.Title.Trim();

        if (title.Length < 2)
        {
            return ServiceResult<SectionResponseDto>.Fail(TitleTooShortMessage, ServiceErrorType.Validation);
        }

        var section = new Section
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = title,
            Position = await _curriculum.NextSectionPositionAsync(courseId),
            CreatedAt = DateTime.UtcNow
        };

        await _curriculum.AddSectionAsync(section);

        return ServiceResult<SectionResponseDto>.Ok(section.ToResponse());
    }

    public async Task<ServiceResult<SectionResponseDto>> UpdateSectionAsync(
        Guid sectionId, Guid userId, SaveSectionRequestDto request)
    {
        var section = await _curriculum.GetSectionAsync(sectionId);

        if (section is null)
        {
            return ServiceResult<SectionResponseDto>.Fail(SectionNotFoundMessage, ServiceErrorType.NotFound);
        }

        var (access, errorType, message) = await AuthorizeOwnerAsync(section.CourseId, userId);

        if (access is null)
        {
            return ServiceResult<SectionResponseDto>.Fail(message, errorType);
        }

        var title = request.Title.Trim();

        if (title.Length < 2)
        {
            return ServiceResult<SectionResponseDto>.Fail(TitleTooShortMessage, ServiceErrorType.Validation);
        }

        section.Title = title;
        section.UpdatedAt = DateTime.UtcNow;
        await _curriculum.SaveChangesAsync();

        return ServiceResult<SectionResponseDto>.Ok(section.ToResponse());
    }

    public async Task<ServiceResult<bool>> DeleteSectionAsync(Guid sectionId, Guid userId)
    {
        var section = await _curriculum.GetSectionAsync(sectionId);

        if (section is null)
        {
            return ServiceResult<bool>.Fail(SectionNotFoundMessage, ServiceErrorType.NotFound);
        }

        var (access, errorType, message) = await AuthorizeOwnerAsync(section.CourseId, userId);

        if (access is null)
        {
            return ServiceResult<bool>.Fail(message, errorType);
        }

        var publishedInSection = await _curriculum.CountPublishedLecturesInSectionAsync(sectionId);

        if (await WouldRemoveLastPublishedLectureAsync(access, publishedInSection))
        {
            return ServiceResult<bool>.Fail(KeepOneLectureMessage, ServiceErrorType.Conflict);
        }

        // Remember which video files belong to this section before the rows disappear.
        var videoKeys = await _curriculum.GetVideoKeysForSectionAsync(sectionId);

        // The lectures inside are deleted by the database (ON DELETE CASCADE).
        var siblings = await _curriculum.GetSectionsForCourseAsync(section.CourseId);
        _curriculum.RemoveSection(section);

        var position = 1;
        foreach (var sibling in siblings.Where(s => s.Id != section.Id))
        {
            sibling.Position = position++;
        }

        await _curriculum.SaveChangesAsync();

        // Only after the database delete succeeded do we delete the files.
        foreach (var key in videoKeys)
        {
            await _storage.DeleteAsync(StorageCategory.Video, key);
        }

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> MoveSectionAsync(Guid sectionId, Guid userId, MoveDirection direction)
    {
        var section = await _curriculum.GetSectionAsync(sectionId);

        if (section is null)
        {
            return ServiceResult<bool>.Fail(SectionNotFoundMessage, ServiceErrorType.NotFound);
        }

        var (access, errorType, message) = await AuthorizeOwnerAsync(section.CourseId, userId);

        if (access is null)
        {
            return ServiceResult<bool>.Fail(message, errorType);
        }

        var siblings = await _curriculum.GetSectionsForCourseAsync(section.CourseId);
        var index = siblings.FindIndex(s => s.Id == section.Id);

        if (index >= 0)
        {
            MoveAndRenumber(siblings, index, direction, (s, position) => s.Position = position);
            await _curriculum.SaveChangesAsync();
        }

        return ServiceResult<bool>.Ok(true);
    }

    // ---------- Lectures ----------

    public async Task<ServiceResult<LectureResponseDto>> CreateLectureAsync(
        Guid sectionId, Guid userId, SaveLectureRequestDto request)
    {
        var section = await _curriculum.GetSectionAsync(sectionId);

        if (section is null)
        {
            return ServiceResult<LectureResponseDto>.Fail(SectionNotFoundMessage, ServiceErrorType.NotFound);
        }

        var (access, errorType, message) = await AuthorizeOwnerAsync(section.CourseId, userId);

        if (access is null)
        {
            return ServiceResult<LectureResponseDto>.Fail(message, errorType);
        }

        var title = request.Title.Trim();

        if (title.Length < 2)
        {
            return ServiceResult<LectureResponseDto>.Fail(TitleTooShortMessage, ServiceErrorType.Validation);
        }

        var lecture = new Lecture
        {
            Id = Guid.NewGuid(),
            SectionId = sectionId,
            Title = title,
            Description = (request.Description ?? string.Empty).Trim(),
            DurationMinutes = request.DurationMinutes,
            Position = await _curriculum.NextLecturePositionAsync(sectionId),
            IsPublished = false,
            CreatedAt = DateTime.UtcNow
        };

        await _curriculum.AddLectureAsync(lecture);

        return ServiceResult<LectureResponseDto>.Ok(lecture.ToResponse());
    }

    public async Task<ServiceResult<LectureResponseDto>> UpdateLectureAsync(
        Guid lectureId, Guid userId, SaveLectureRequestDto request)
    {
        var lecture = await _curriculum.GetLectureAsync(lectureId);

        if (lecture is null)
        {
            return ServiceResult<LectureResponseDto>.Fail(LectureNotFoundMessage, ServiceErrorType.NotFound);
        }

        var (access, errorType, message) = await AuthorizeOwnerAsync(lecture.Section.CourseId, userId);

        if (access is null)
        {
            return ServiceResult<LectureResponseDto>.Fail(message, errorType);
        }

        var title = request.Title.Trim();

        if (title.Length < 2)
        {
            return ServiceResult<LectureResponseDto>.Fail(TitleTooShortMessage, ServiceErrorType.Validation);
        }

        lecture.Title = title;
        lecture.Description = (request.Description ?? string.Empty).Trim();
        lecture.DurationMinutes = request.DurationMinutes;
        lecture.UpdatedAt = DateTime.UtcNow;
        await _curriculum.SaveChangesAsync();

        return ServiceResult<LectureResponseDto>.Ok(lecture.ToResponse());
    }

    public async Task<ServiceResult<bool>> DeleteLectureAsync(Guid lectureId, Guid userId)
    {
        var lecture = await _curriculum.GetLectureAsync(lectureId);

        if (lecture is null)
        {
            return ServiceResult<bool>.Fail(LectureNotFoundMessage, ServiceErrorType.NotFound);
        }

        var (access, errorType, message) = await AuthorizeOwnerAsync(lecture.Section.CourseId, userId);

        if (access is null)
        {
            return ServiceResult<bool>.Fail(message, errorType);
        }

        if (await WouldRemoveLastPublishedLectureAsync(access, lecture.IsPublished ? 1 : 0))
        {
            return ServiceResult<bool>.Fail(KeepOneLectureMessage, ServiceErrorType.Conflict);
        }

        var videoKey = lecture.VideoPath;

        var siblings = await _curriculum.GetLecturesForSectionAsync(lecture.SectionId);
        _curriculum.RemoveLecture(lecture);

        var position = 1;
        foreach (var sibling in siblings.Where(l => l.Id != lecture.Id))
        {
            sibling.Position = position++;
        }

        await _curriculum.SaveChangesAsync();

        // Only after the database delete succeeded do we delete the file.
        if (!string.IsNullOrEmpty(videoKey))
        {
            await _storage.DeleteAsync(StorageCategory.Video, videoKey);
        }

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<LectureResponseDto>> SetLecturePublishedAsync(
        Guid lectureId, Guid userId, bool publish)
    {
        var lecture = await _curriculum.GetLectureAsync(lectureId);

        if (lecture is null)
        {
            return ServiceResult<LectureResponseDto>.Fail(LectureNotFoundMessage, ServiceErrorType.NotFound);
        }

        var (access, errorType, message) = await AuthorizeOwnerAsync(lecture.Section.CourseId, userId);

        if (access is null)
        {
            return ServiceResult<LectureResponseDto>.Fail(message, errorType);
        }

        if (lecture.IsPublished != publish)
        {
            if (!publish && await WouldRemoveLastPublishedLectureAsync(access, 1))
            {
                return ServiceResult<LectureResponseDto>.Fail(KeepOneLectureMessage, ServiceErrorType.Conflict);
            }

            lecture.IsPublished = publish;
            lecture.UpdatedAt = DateTime.UtcNow;
            await _curriculum.SaveChangesAsync();
        }

        return ServiceResult<LectureResponseDto>.Ok(lecture.ToResponse());
    }

    public async Task<ServiceResult<bool>> MoveLectureAsync(Guid lectureId, Guid userId, MoveDirection direction)
    {
        var lecture = await _curriculum.GetLectureAsync(lectureId);

        if (lecture is null)
        {
            return ServiceResult<bool>.Fail(LectureNotFoundMessage, ServiceErrorType.NotFound);
        }

        var (access, errorType, message) = await AuthorizeOwnerAsync(lecture.Section.CourseId, userId);

        if (access is null)
        {
            return ServiceResult<bool>.Fail(message, errorType);
        }

        var siblings = await _curriculum.GetLecturesForSectionAsync(lecture.SectionId);
        var index = siblings.FindIndex(l => l.Id == lecture.Id);

        if (index >= 0)
        {
            MoveAndRenumber(siblings, index, direction, (l, position) => l.Position = position);
            await _curriculum.SaveChangesAsync();
        }

        return ServiceResult<bool>.Ok(true);
    }

    // ---------- Helpers ----------

    // Only the course owner may change its curriculum.
    private async Task<(CourseAccess? Access, ServiceErrorType ErrorType, string Message)> AuthorizeOwnerAsync(
        Guid courseId, Guid userId)
    {
        var access = await _curriculum.GetCourseAccessAsync(courseId);
        var (allowed, errorType, message) = CourseOwnership.Check(access, userId);

        return allowed ? (access, ServiceErrorType.None, string.Empty) : (null, errorType, message);
    }

    // True if removing this many published lectures would leave a published course with none.
    private async Task<bool> WouldRemoveLastPublishedLectureAsync(CourseAccess access, int publishedBeingRemoved)
    {
        if (access.Status != CourseStatus.published || publishedBeingRemoved <= 0)
        {
            return false;
        }

        var totalPublished = await _curriculum.CountPublishedLecturesAsync(access.Id);

        return totalPublished - publishedBeingRemoved <= 0;
    }

    // Swaps the item with its neighbour (if there is one), then renumbers everything 1, 2, 3...
    private static void MoveAndRenumber<T>(
        List<T> items, int index, MoveDirection direction, Action<T, int> setPosition)
    {
        var newIndex = direction == MoveDirection.Up ? index - 1 : index + 1;

        if (newIndex >= 0 && newIndex < items.Count)
        {
            (items[index], items[newIndex]) = (items[newIndex], items[index]);
        }

        for (var i = 0; i < items.Count; i++)
        {
            setPosition(items[i], i + 1);
        }
    }
}