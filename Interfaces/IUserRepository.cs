using LMS.API.Enums;
using LMS.API.Models;

namespace LMS.API.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(Guid id);
        Task<(List<User> Items, int TotalCount)> GetPagedAsync(string? search, UserRole? role, int page, int pageSize);
        Task AddAsync(User user);
        Task UpdateAsync(User user);

    }
}
