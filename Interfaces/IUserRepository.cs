using LMS.API.Enums;
using LMS.API.Models;

namespace LMS.API.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task<bool> EmailExistsForOtherUserAsync(string email, Guid excludeUserId);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<(List<User> Items, int TotalCount)> GetPagedAsync(string? search, UserRole? role, int page, int pageSize);
    Task<bool> HasCoursesAsync(Guid userId);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}