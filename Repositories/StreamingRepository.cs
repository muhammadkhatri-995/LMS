using LMS.API.Common;
using LMS.API.Data;
using LMS.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Repositories;

public class StreamingRepository : IStreamingRepository
{
    private readonly AppDbContext _db;

    public StreamingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LectureStreamInfo?> GetLectureStreamInfoAsync(Guid lectureId)
    {
        return await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Id == lectureId)
            .Select(l => new LectureStreamInfo(
                l.Id,
                l.VideoPath,
                l.IsPublished,
                l.Section.Course.InstructorId,
                l.Section.Course.Status))
            .FirstOrDefaultAsync();
    }

    public async Task<UserStatus?> GetUserStatusAsync(Guid userId)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserStatus(u.IsActive, u.Role))
            .FirstOrDefaultAsync();
    }
}