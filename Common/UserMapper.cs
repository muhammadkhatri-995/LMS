using LMS.API.DTOs;
using LMS.API.Models;

namespace LMS.API.Common;

public static class UserMapper
{
    public static UserResponseDto ToResponse(this User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}