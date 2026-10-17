using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Models;

namespace LMS.API.Interfaces;

public interface ICurriculumRepository
{
    // Read-only views
    Task<CourseAccess?> GetCourseAccessAsync(Guid courseId);
    Task<List<SectionResponseDto>> GetCurriculumAsync(Guid courseId, bool includeUnpublished);

    // Tracked entities, used when we change or delete them
    Task<Section?> GetSectionAsync(Guid sectionId);
    Task<Lecture?> GetLectureAsync(Guid lectureId);
    Task<List<Section>> GetSectionsForCourseAsync(Guid courseId);
    Task<List<Lecture>> GetLecturesForSectionAsync(Guid sectionId);

    // Storage keys of uploaded videos, so their files can be deleted along with the rows
    Task<List<string>> GetVideoKeysForCourseAsync(Guid courseId);
    Task<List<string>> GetVideoKeysForSectionAsync(Guid sectionId);

    // Counts and positions
    Task<int> CountPublishedLecturesAsync(Guid courseId);
    Task<int> CountPublishedLecturesInSectionAsync(Guid sectionId);
    Task<int> NextSectionPositionAsync(Guid courseId);
    Task<int> NextLecturePositionAsync(Guid sectionId);

    // Changes
    Task AddSectionAsync(Section section);
    Task AddLectureAsync(Lecture lecture);
    void RemoveSection(Section section);
    void RemoveLecture(Lecture lecture);
    Task SaveChangesAsync();
}