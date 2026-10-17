using LMS.API.Common;

namespace LMS.API.Interfaces;

public interface IStreamingRepository
{
    Task<LectureStreamInfo?> GetLectureStreamInfoAsync(Guid lectureId);
    Task<UserStatus?> GetUserStatusAsync(Guid userId);
}