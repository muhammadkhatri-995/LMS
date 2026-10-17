using LMS.API.DTOs;
using LMS.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers;

[Authorize]
[Route("api")]
public class CurriculumController : ApiControllerBase
{
    private readonly ICurriculumService _curriculumService;

    public CurriculumController(ICurriculumService curriculumService)
    {
        _curriculumService = curriculumService;
    }

    // ---------- Reading (any logged-in user, filtered by the service) ----------

    [HttpGet("courses/{courseId:guid}/curriculum")]
    public async Task<IActionResult> GetCurriculum(Guid courseId)
    {
        var result = await _curriculumService.GetCurriculumAsync(courseId, CurrentUserId, IsAdmin);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    // ---------- Sections (instructor who owns the course) ----------

    [Authorize(Roles = "Instructor")]
    [HttpPost("courses/{courseId:guid}/sections")]
    public async Task<IActionResult> CreateSection(Guid courseId, [FromBody] SaveSectionRequestDto request)
    {
        var result = await _curriculumService.CreateSectionAsync(courseId, CurrentUserId, request);

        if (!result.Success)
        {
            return ToErrorResponse(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPut("sections/{id:guid}")]
    public async Task<IActionResult> UpdateSection(Guid id, [FromBody] SaveSectionRequestDto request)
    {
        var result = await _curriculumService.UpdateSectionAsync(id, CurrentUserId, request);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [Authorize(Roles = "Instructor")]
    [HttpDelete("sections/{id:guid}")]
    public async Task<IActionResult> DeleteSection(Guid id)
    {
        var result = await _curriculumService.DeleteSectionAsync(id, CurrentUserId);
        return result.Success ? NoContent() : ToErrorResponse(result);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPost("sections/{id:guid}/move")]
    public async Task<IActionResult> MoveSection(Guid id, [FromBody] MoveRequestDto request)
    {
        var result = await _curriculumService.MoveSectionAsync(id, CurrentUserId, request.Direction!.Value);
        return result.Success ? NoContent() : ToErrorResponse(result);
    }

    // ---------- Lectures (instructor who owns the course) ----------

    [Authorize(Roles = "Instructor")]
    [HttpPost("sections/{sectionId:guid}/lectures")]
    public async Task<IActionResult> CreateLecture(Guid sectionId, [FromBody] SaveLectureRequestDto request)
    {
        var result = await _curriculumService.CreateLectureAsync(sectionId, CurrentUserId, request);

        if (!result.Success)
        {
            return ToErrorResponse(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPut("lectures/{id:guid}")]
    public async Task<IActionResult> UpdateLecture(Guid id, [FromBody] SaveLectureRequestDto request)
    {
        var result = await _curriculumService.UpdateLectureAsync(id, CurrentUserId, request);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [Authorize(Roles = "Instructor")]
    [HttpDelete("lectures/{id:guid}")]
    public async Task<IActionResult> DeleteLecture(Guid id)
    {
        var result = await _curriculumService.DeleteLectureAsync(id, CurrentUserId);
        return result.Success ? NoContent() : ToErrorResponse(result);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPost("lectures/{id:guid}/publish")]
    public async Task<IActionResult> PublishLecture(Guid id)
    {
        var result = await _curriculumService.SetLecturePublishedAsync(id, CurrentUserId, true);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPost("lectures/{id:guid}/unpublish")]
    public async Task<IActionResult> UnpublishLecture(Guid id)
    {
        var result = await _curriculumService.SetLecturePublishedAsync(id, CurrentUserId, false);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPost("lectures/{id:guid}/move")]
    public async Task<IActionResult> MoveLecture(Guid id, [FromBody] MoveRequestDto request)
    {
        var result = await _curriculumService.MoveLectureAsync(id, CurrentUserId, request.Direction!.Value);
        return result.Success ? NoContent() : ToErrorResponse(result);
    }
}