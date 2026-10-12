using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Enums;
using LMS.API.Interfaces;
using LMS.API.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LMS.API.Services;

public class CategoryService : ICategoryService
{
    private const string NotFoundMessage = "Category not found.";
    private const string DuplicateMessage = "A category with this name already exists.";

    private readonly ICategoryRepository _categories;

    public CategoryService(ICategoryRepository categories)
    {
        _categories = categories;
    }

    public async Task<ServiceResult<List<CategoryResponseDto>>> GetAllAsync()
    {
        var categories = await _categories.GetAllAsync();
        var result = categories.Select(ToDto).ToList();

        return ServiceResult<List<CategoryResponseDto>>.Ok(result);
    }

    public async Task<ServiceResult<CategoryResponseDto>> CreateAsync(SaveCategoryRequestDto request)
    {
        var name = request.Name.Trim();

        if (name.Length < 2)
        {
            return ServiceResult<CategoryResponseDto>.Fail(
                "Name must be at least 2 characters.", ServiceErrorType.Validation);
        }

        if (await _categories.NameExistsAsync(name, null))
        {
            return ServiceResult<CategoryResponseDto>.Fail(DuplicateMessage, ServiceErrorType.Conflict);
        }

        var category = new Category { Id = Guid.NewGuid(), Name = name, CreatedAt = DateTime.UtcNow };

        try
        {
            await _categories.AddAsync(category);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return ServiceResult<CategoryResponseDto>.Fail(DuplicateMessage, ServiceErrorType.Conflict);
        }

        return ServiceResult<CategoryResponseDto>.Ok(ToDto(category));
    }

    public async Task<ServiceResult<CategoryResponseDto>> UpdateAsync(Guid id, SaveCategoryRequestDto request)
    {
        var category = await _categories.GetByIdAsync(id);

        if (category is null)
        {
            return ServiceResult<CategoryResponseDto>.Fail(NotFoundMessage, ServiceErrorType.NotFound);
        }

        var name = request.Name.Trim();

        if (name.Length < 2)
        {
            return ServiceResult<CategoryResponseDto>.Fail(
                "Name must be at least 2 characters.", ServiceErrorType.Validation);
        }

        if (await _categories.NameExistsAsync(name, id))
        {
            return ServiceResult<CategoryResponseDto>.Fail(DuplicateMessage, ServiceErrorType.Conflict);
        }

        category.Name = name;

        try
        {
            await _categories.UpdateAsync(category);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return ServiceResult<CategoryResponseDto>.Fail(DuplicateMessage, ServiceErrorType.Conflict);
        }

        return ServiceResult<CategoryResponseDto>.Ok(ToDto(category));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        var category = await _categories.GetByIdAsync(id);

        if (category is null)
        {
            return ServiceResult<bool>.Fail(NotFoundMessage, ServiceErrorType.NotFound);
        }

        if (await _categories.HasCoursesAsync(id))
        {
            return ServiceResult<bool>.Fail(
                "This category is used by courses and cannot be deleted.", ServiceErrorType.Conflict);
        }

        await _categories.DeleteAsync(category);

        return ServiceResult<bool>.Ok(true);
    }

    private static CategoryResponseDto ToDto(Category category)
    {
        return new CategoryResponseDto { Id = category.Id, Name = category.Name };
    }
}