using LMS.API.Common;
using LMS.API.DTOs;

namespace LMS.API.Interfaces;

public interface ICategoryService
{
    Task<ServiceResult<List<CategoryResponseDto>>> GetAllAsync();
    Task<ServiceResult<CategoryResponseDto>> CreateAsync(SaveCategoryRequestDto request);
    Task<ServiceResult<CategoryResponseDto>> UpdateAsync(Guid id, SaveCategoryRequestDto request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}