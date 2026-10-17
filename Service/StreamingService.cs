using LMS.API.Common;
using LMS.API.DTOs;
using LMS.API.Enums;
using LMS.API.Interfaces;

namespace LMS.API.Services;

public class StreamingService : IStreamingService
{
    private const string LectureNotFoundMessage = "Lecture not found.";

    private readonly IStreamingRepository _repository;
    private readonly IStreamTicketService _tickets;
    private readonly IStorageService _storage;

    public StreamingService(
        IStreamingRepository repository,
        IStreamTicketService tickets,
        IStorageService storage)
    {
        _repository = repository;
        _tickets = tickets;
        _storage = storage;
    }

    public async Task<ServiceResult<StreamTicketResponseDto>> CreateTicketAsync(Guid lectureId, Guid userId)
    {
        var (info, errorType, message) = await CheckAccessAsync(lectureId, userId);

        if (info is null)
        {
            return ServiceResult<StreamTicketResponseDto>.Fail(message, errorType);
        }

        var (ticket, expiresAt) = _tickets.Create(lectureId, userId);

        return ServiceResult<StreamTicketResponseDto>.Ok(new StreamTicketResponseDto
        {
            Url = "/api/lectures/" + lectureId + "/stream?ticket=" + ticket,
            ExpiresAt = expiresAt
        });
    }

    public async Task<ServiceResult<VideoSource>> OpenStreamAsync(Guid lectureId, string? ticket)
    {
        if (!_tickets.TryValidate(ticket, lectureId, out var userId))
        {
            return ServiceResult<VideoSource>.Fail(
                "The stream ticket is missing, invalid, or expired.", ServiceErrorType.Unauthorized);
        }

        // The ticket proves who asked and for what, not that they may still watch it.
        // Every request re-checks the database, so a deactivation or unpublish takes effect at once.
        var (info, errorType, message) = await CheckAccessAsync(lectureId, userId);

        if (info is null)
        {
            return ServiceResult<VideoSource>.Fail(message, errorType);
        }

        var content = await _storage.OpenReadAsync(StorageCategory.Video, info.VideoPath!);

        if (content is null)
        {
            return ServiceResult<VideoSource>.Fail("The video file is missing on the server.", ServiceErrorType.NotFound);
        }

        return ServiceResult<VideoSource>.Ok(new VideoSource(content, ContentTypeFor(info.VideoPath!)));
    }

    // ---------- Helpers ----------

    // Returns the lecture info only if this user may watch it right now.
    private async Task<(LectureStreamInfo? Info, ServiceErrorType ErrorType, string Message)> CheckAccessAsync(
        Guid lectureId, Guid userId)
    {
        var user = await _repository.GetUserStatusAsync(userId);

        if (user is null || !user.IsActive)
        {
            return (null, ServiceErrorType.Unauthorized, "This account is not active.");
        }

        var info = await _repository.GetLectureStreamInfoAsync(lectureId);

        if (info is null)
        {
            return (null, ServiceErrorType.NotFound, LectureNotFoundMessage);
        }

        var isAdmin = user.Role == UserRole.Admin;
        var isOwner = info.InstructorId == userId;
        var isPublic = info.IsPublished && info.CourseStatus == CourseStatus.published;

        // Module 6 will add: a student must also be enrolled in the course.
        if (!isAdmin && !isOwner && !isPublic)
        {
            // Drafts are private. Anyone else is told the lecture does not exist.
            return (null, ServiceErrorType.NotFound, LectureNotFoundMessage);
        }

        if (string.IsNullOrEmpty(info.VideoPath))
        {
            return (null, ServiceErrorType.NotFound, "This lecture has no video yet.");
        }

        return (info, ServiceErrorType.None, string.Empty);
    }

    // Browsers refuse to play a video sent with the wrong type, so this must match the file.
    private static string ContentTypeFor(string key)
    {
        return Path.GetExtension(key).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }
}