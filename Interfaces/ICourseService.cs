using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Enums;

namespace LMS.API.Interfaces;

public interface ICourseService
{
    Task<ServiceResult<PagedResult<CourseResponseDto>>> GetPublishedAsync(
        string? search, Guid? categoryId, int page, int pageSize);

    Task<ServiceResult<PagedResult<CourseResponseDto>>> GetMineAsync(
        Guid instructorId, string? search, CourseStatus? status, int page, int pageSize);

    Task<ServiceResult<PagedResult<CourseResponseDto>>> GetAllAsync(
        string? search, CourseStatus? status, Guid? categoryId, int page, int pageSize);

    Task<ServiceResult<CourseResponseDto>> GetByIdAsync(Guid courseId, Guid userId, bool isAdmin);
    Task<ServiceResult<CourseResponseDto>> CreateAsync(Guid instructorId, SaveCourseRequestDto request);
    Task<ServiceResult<CourseResponseDto>> UpdateAsync(Guid courseId, Guid userId, SaveCourseRequestDto request);
    Task<ServiceResult<CourseResponseDto>> SetPublishedAsync(Guid courseId, Guid userId, bool isAdmin, bool publish);
    Task<ServiceResult<bool>> DeleteAsync(Guid courseId, Guid userId, bool isAdmin);
}