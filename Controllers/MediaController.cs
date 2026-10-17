using LMS.API.Common;
using LMS.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers;

[Authorize(Roles = "Instructor")]
[Route("api")]
public class MediaController : ApiControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    // ---------- Course thumbnail ----------

    [HttpPut("courses/{courseId:guid}/thumbnail")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MediaLimits.MaxThumbnailBytes + MediaLimits.MultipartOverheadBytes)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = MediaLimits.MaxThumbnailBytes + MediaLimits.MultipartOverheadBytes,
        BufferBodyLengthLimit = MediaLimits.MaxThumbnailBytes + MediaLimits.MultipartOverheadBytes)]
    public async Task<IActionResult> UploadThumbnail(Guid courseId, [FromForm] IFormFile file)
    {
        var result = await _mediaService.UploadThumbnailAsync(courseId, CurrentUserId, file);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [HttpDelete("courses/{courseId:guid}/thumbnail")]
    public async Task<IActionResult> RemoveThumbnail(Guid courseId)
    {
        var result = await _mediaService.RemoveThumbnailAsync(courseId, CurrentUserId);
        return result.Success ? NoContent() : ToErrorResponse(result);
    }

    // ---------- Lecture video ----------

    [HttpPut("lectures/{lectureId:guid}/video")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MediaLimits.MaxVideoBytes + MediaLimits.MultipartOverheadBytes)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = MediaLimits.MaxVideoBytes + MediaLimits.MultipartOverheadBytes,
        BufferBodyLengthLimit = MediaLimits.MaxVideoBytes + MediaLimits.MultipartOverheadBytes)]
    public async Task<IActionResult> UploadVideo(Guid lectureId, [FromForm] IFormFile file)
    {
        var result = await _mediaService.UploadVideoAsync(lectureId, CurrentUserId, file);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [HttpDelete("lectures/{lectureId:guid}/video")]
    public async Task<IActionResult> RemoveVideo(Guid lectureId)
    {
        var result = await _mediaService.RemoveVideoAsync(lectureId, CurrentUserId);
        return result.Success ? NoContent() : ToErrorResponse(result);
    }
}