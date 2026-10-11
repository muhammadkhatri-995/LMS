using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Enums;
using LMS.API.Interfaces;
using LMS.API.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LMS.API.Services;

public class AuthService : IAuthService
{
    private const string DuplicateEmailMessage = "An account with this email already exists.";
    private const string InvalidLoginMessage = "Invalid email or password.";

    // Used when the email doesn't exist, so the response time looks the same
    // as a real password check. This stops attackers from discovering which
    // emails are registered by measuring how fast the API answers.
    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword("dummy-password-for-timing", workFactor: 12);

    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;

    public AuthService(IUserRepository users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    public async Task<ServiceResult<UserResponseDto>> RegisterAsync(RegisterRequestDto request)
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
            return ServiceResult<UserResponseDto>.Fail(DuplicateEmailMessage, ServiceErrorType.Conflict);
        }

        return ServiceResult<UserResponseDto>.Ok(ToDto(user));
    }

    public async Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email);

        var hashToCheck = user?.PasswordHash ?? DummyHash;
        var passwordIsCorrect = BCrypt.Net.BCrypt.Verify(request.Password, hashToCheck);

        if (user is null || !passwordIsCorrect)
        {
            return ServiceResult<LoginResponseDto>.Fail(InvalidLoginMessage, ServiceErrorType.Unauthorized);
        }

        if (!user.IsActive)
        {
            return ServiceResult<LoginResponseDto>.Fail(
                "This account has been deactivated. Please contact an administrator.",
                ServiceErrorType.Forbidden);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        var (token, expiresAt) = _tokens.CreateToken(user);

        return ServiceResult<LoginResponseDto>.Ok(new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = ToDto(user)
        });
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