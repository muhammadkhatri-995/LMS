using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Enums;
using LMS.API.Interfaces;
using LMS.API.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LMS.API.Services;

public class UserService : IUserService
{
    private const string UserNotFoundMessage = "User not found.";
    private const string DuplicateEmailMessage = "An account with this email already exists.";

    private readonly IUserRepository _users;

    public UserService(IUserRepository users)
    {
        _users = users;
    }

    public async Task<ServiceResult<UserResponseDto>> GetProfileAsync(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId);

        if (user is null)
        {
            return ServiceResult<UserResponseDto>.Fail(UserNotFoundMessage, ServiceErrorType.NotFound);
        }

        return ServiceResult<UserResponseDto>.Ok(user.ToResponse());
    }

    public async Task<ServiceResult<UserResponseDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request)
    {
        var user = await _users.GetByIdAsync(userId);

        if (user is null)
        {
            return ServiceResult<UserResponseDto>.Fail(UserNotFoundMessage, ServiceErrorType.NotFound);
        }

        user.FullName = request.FullName.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        return ServiceResult<UserResponseDto>.Ok(user.ToResponse());
    }

    public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request)
    {
        var user = await _users.GetByIdAsync(userId);

        if (user is null)
        {
            return ServiceResult<bool>.Fail(UserNotFoundMessage, ServiceErrorType.NotFound);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return ServiceResult<bool>.Fail("Current password is incorrect.", ServiceErrorType.Validation);
        }

        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
        {
            return ServiceResult<bool>.Fail(
                "New password must be different from the current password.",
                ServiceErrorType.Validation);
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<PagedResult<UserResponseDto>>> GetUsersAsync(
        string? search, UserRole? role, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _users.GetPagedAsync(search, role, page, pageSize);

        var result = new PagedResult<UserResponseDto>
        {
            Items = items.Select(u => u.ToResponse()).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<UserResponseDto>>.Ok(result);
    }

    public async Task<ServiceResult<UserResponseDto>> CreateUserAsync(CreateUserRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.EmailExistsAsync(email))
        {
            return ServiceResult<UserResponseDto>.Fail(DuplicateEmailMessage, ServiceErrorType.Conflict);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Role = request.Role!.Value,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _users.AddAsync(user);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return ServiceResult<UserResponseDto>.Fail(DuplicateEmailMessage, ServiceErrorType.Conflict);
        }

        return ServiceResult<UserResponseDto>.Ok(user.ToResponse());
    }

    public async Task<ServiceResult<UserResponseDto>> SetStatusAsync(Guid targetUserId, bool isActive, Guid currentUserId)
    {
        if (targetUserId == currentUserId && !isActive)
        {
            return ServiceResult<UserResponseDto>.Fail(
                "You cannot deactivate your own account.",
                ServiceErrorType.Validation);
        }

        var user = await _users.GetByIdAsync(targetUserId);

        if (user is null)
        {
            return ServiceResult<UserResponseDto>.Fail(UserNotFoundMessage, ServiceErrorType.NotFound);
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        return ServiceResult<UserResponseDto>.Ok(user.ToResponse());
    }
}