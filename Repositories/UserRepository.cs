using LMS.API.Common;
using LMS.API.Data;
using LMS.API.Enums;
using LMS.API.Interfaces;
using LMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _db.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsForOtherUserAsync(string email, Guid excludeUserId)
    {
        return await _db.Users.AnyAsync(u => u.Email == email && u.Id != excludeUserId);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<(List<User> Items, int TotalCount)> GetPagedAsync(
        string? search, UserRole? role, int page, int pageSize)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = SqlLike.ContainsPattern(search);

            query = query.Where(u =>
                EF.Functions.ILike(u.FullName, pattern, "\\") ||
                EF.Functions.ILike(u.Email, pattern, "\\"));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> HasCoursesAsync(Guid userId)
    {
        return await _db.Courses.AnyAsync(c => c.InstructorId == userId);
    }

    public async Task AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }
}