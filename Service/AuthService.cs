using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Interfaces;
using LMS.API.Models;
using LMS.API.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;



namespace LMS.API.Service
{
    public class AuthService : IAuthService
    {
        private const string DuplicateEmailMessage = "An account with this email already exists.";

        private readonly IUserRepository _users;

        public AuthService(IUserRepository users)
        {
            _users = users;
        }
        public async Task<ServiceResult<UserResponseDto>> RegisterAsync(RegisterRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (await _users.EmailExistsAsync(email))
            {
                return ServiceResult<UserResponseDto>.Fail(DuplicateEmailMessage);
            }
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            try
            {
                await _users.AddAsync(user);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
            {
                // Two people registered the same email at the same moment.
                // The unique index in the database stopped the second one.
                return ServiceResult<UserResponseDto>.Fail(DuplicateEmailMessage);
            }

            return ServiceResult<UserResponseDto>.Ok(ToDto(user));
        }

        private static UserResponseDto ToDto(User user)
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
    }
