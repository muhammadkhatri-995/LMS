using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Enums;

namespace LMS.API.Interfaces;

public interface ICurriculumService
{
    Task<ServiceResult<CurriculumResponseDto>> GetCurriculumAsync(Guid courseId, Guid userId, bool isAdmin);

    Task<ServiceResult<SectionResponseDto>> CreateSectionAsync(Guid courseId, Guid userId, SaveSectionRequestDto request);
    Task<ServiceResult<SectionResponseDto>> UpdateSectionAsync(Guid sectionId, Guid userId, SaveSectionRequestDto request);
    Task<ServiceResult<bool>> DeleteSectionAsync(Guid sectionId, Guid userId);
    Task<ServiceResult<bool>> MoveSectionAsync(Guid sectionId, Guid userId, MoveDirection direction);

    Task<ServiceResult<LectureResponseDto>> CreateLectureAsync(Guid sectionId, Guid userId, SaveLectureRequestDto request);
    Task<ServiceResult<LectureResponseDto>> UpdateLectureAsync(Guid lectureId, Guid userId, SaveLectureRequestDto request);
    Task<ServiceResult<bool>> DeleteLectureAsync(Guid lectureId, Guid userId);
    Task<ServiceResult<LectureResponseDto>> SetLecturePublishedAsync(Guid lectureId, Guid userId, bool publish);
    Task<ServiceResult<bool>> MoveLectureAsync(Guid lectureId, Guid userId, MoveDirection direction);
}