//using LMS.API.Enums;
//using LMS.API.Models;

//namespace LMS.API.Interfaces
//{
//    public interface IUserRepository
//    {
//        Task<bool> EmailExistsAsync(string email);
//        Task<User?> GetByEmailAsync(string email);
//        Task<User?> GetByIdAsync(Guid id);
//        Task<(List<User> Items, int TotalCount)> GetPagedAsync(string? search, UserRole? role, int page, int pageSize);
//        Task AddAsync(User user);
//        Task UpdateAsync(User user);
//    }

//}


using LMS.API.Enums;
using LMS.API.Models;

namespace LMS.API.Interfaces
{
    public interface IUserRepository
    {
        // READ: Check whether an email already exists
        Task<bool> EmailExistsAsync(string email);

        // READ: Get a user by email
        Task<User?> GetByEmailAsync(string email);

        // READ: Get a user by ID
        Task<User?> GetByIdAsync(Guid id);

        // READ: Get users with pagination, search, and role filtering
        Task<(List<User> Items, int TotalCount)> GetPagedAsync(
            string? search,
            UserRole? role,
            int page,
            int pageSize);

        // CREATE: Add a new user
        Task AddAsync(User user);

        // UPDATE: Update an existing user
        Task UpdateAsync(User user);

        // DELETE: Delete a user by ID
        Task DeleteAsync(Guid id);
    }
}







