using LMS.API.DTOs;
using LMS.API.Enums;
using LMS.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers;

[Authorize]
[Route("api/courses")]
public class CoursesController : ApiControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    // Published courses, for any logged-in user.
    [HttpGet]
    public async Task<IActionResult> GetPublished(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var result = await _courseService.GetPublishedAsync(search, categoryId, page, pageSize);
        return Ok(result.Data);
    }

    // The instructor's own courses, drafts included.
    [Authorize(Roles = "Instructor")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] string? search,
        [FromQuery] CourseStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _courseService.GetMineAsync(CurrentUserId, search, status, page, pageSize);
        return Ok(result.Data);
    }

    // Every course, for moderation.
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] CourseStatus? status,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _courseService.GetAllAsync(search, status, categoryId, page, pageSize);
        return Ok(result.Data);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _courseService.GetByIdAsync(id, CurrentUserId, IsAdmin);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveCourseRequestDto request)
    {
        var result = await _courseService.CreateAsync(CurrentUserId, request);

        if (!result.Success)
        {
            return ToErrorResponse(result);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveCourseRequestDto request)
    {
        var result = await _courseService.UpdateAsync(id, CurrentUserId, request);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var result = await _courseService.SetPublishedAsync(id, CurrentUserId, IsAdmin, true);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpPost("{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id)
    {
        var result = await _courseService.SetPublishedAsync(id, CurrentUserId, IsAdmin, false);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _courseService.DeleteAsync(id, CurrentUserId, IsAdmin);
        return result.Success ? NoContent() : ToErrorResponse(result);
    }
}