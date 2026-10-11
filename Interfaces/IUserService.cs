using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Enums;
namespace LMS.API.Interfaces;

public interface IUserService
{
    Task<ServiceResult<UserResponseDto>> GetProfileAsync(Guid userId);
    Task<ServiceResult<UserResponseDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request);
    Task<ServiceResult<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request);

    Task<ServiceResult<PagedResult<UserResponseDto>>> GetUsersAsync(string? search, UserRole? role, int page, int pageSize);
    Task<ServiceResult<UserResponseDto>> CreateUserAsync(CreateUserRequestDto request);
    Task<ServiceResult<UserResponseDto>> SetStatusAsync(Guid targetUserId, bool isActive, Guid currentUserId);
}