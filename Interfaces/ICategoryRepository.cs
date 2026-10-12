using LMS.API.Models;

namespace LMS.API.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(Guid id);
    Task<bool> NameExistsAsync(string name, Guid? excludeId);
    Task<bool> HasCoursesAsync(Guid id);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(Category category);
}