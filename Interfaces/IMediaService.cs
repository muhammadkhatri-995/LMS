using LMS.API.Common;
using LMS.API.DTOs;
using Microsoft.AspNetCore.Http;

namespace LMS.API.Interfaces;

public interface IMediaService
{
    Task<ServiceResult<ThumbnailResponseDto>> UploadThumbnailAsync(Guid courseId, Guid userId, IFormFile file);
    Task<ServiceResult<bool>> RemoveThumbnailAsync(Guid courseId, Guid userId);

    Task<ServiceResult<LectureResponseDto>> UploadVideoAsync(Guid lectureId, Guid userId, IFormFile file);
    Task<ServiceResult<bool>> RemoveVideoAsync(Guid lectureId, Guid userId);
}