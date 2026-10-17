using LMS.API.Common;
using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Interfaces;
using LMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Repositories;

public class CurriculumRepository : ICurriculumRepository
{
    private readonly AppDbContext _db;

    public CurriculumRepository(AppDbContext db)
    {
        _db = db;
    }

    // ---------- Read-only views ----------

    public async Task<CourseAccess?> GetCourseAccessAsync(Guid courseId)
    {
        return await _db.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new CourseAccess(c.Id, c.InstructorId, c.Status))
            .FirstOrDefaultAsync();
    }

    public async Task<List<SectionResponseDto>> GetCurriculumAsync(Guid courseId, bool includeUnpublished)
    {
        var sections = _db.Sections.AsNoTracking().Where(s => s.CourseId == courseId);

        // Students only see sections that have at least one published lecture.
        if (!includeUnpublished)
        {
            sections = sections.Where(s => s.Lectures.Any(l => l.IsPublished));
        }

        return await sections
            .OrderBy(s => s.Position)
            .ThenBy(s => s.CreatedAt)
            .Select(s => new SectionResponseDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                Title = s.Title,
                Position = s.Position,
                Lectures = s.Lectures
                    .Where(l => includeUnpublished || l.IsPublished)
                    .OrderBy(l => l.Position)
                    .ThenBy(l => l.CreatedAt)
                    .Select(l => new LectureResponseDto
                    {
                        Id = l.Id,
                        SectionId = l.SectionId,
                        Title = l.Title,
                        Description = l.Description,
                        DurationMinutes = l.DurationMinutes,
                        Position = l.Position,
                        IsPublished = l.IsPublished,
                        HasVideo = l.VideoPath != null && l.VideoPath != "",
                        CreatedAt = l.CreatedAt,
                        UpdatedAt = l.UpdatedAt
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    // ---------- Tracked entities ----------

    public async Task<Section?> GetSectionAsync(Guid sectionId)
    {
        return await _db.Sections.FirstOrDefaultAsync(s => s.Id == sectionId);
    }

    public async Task<Lecture?> GetLectureAsync(Guid lectureId)
    {
        return await _db.Lectures
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lectureId);
    }

    public async Task<List<Section>> GetSectionsForCourseAsync(Guid courseId)
    {
        return await _db.Sections
            .Where(s => s.CourseId == courseId)
            .OrderBy(s => s.Position)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Lecture>> GetLecturesForSectionAsync(Guid sectionId)
    {
        return await _db.Lectures
            .Where(l => l.SectionId == sectionId)
            .OrderBy(l => l.Position)
            .ThenBy(l => l.CreatedAt)
            .ToListAsync();
    }

    // ---------- Stored video keys ----------

    public async Task<List<string>> GetVideoKeysForCourseAsync(Guid courseId)
    {
        return await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Section.CourseId == courseId && l.VideoPath != null && l.VideoPath != "")
            .Select(l => l.VideoPath!)
            .ToListAsync();
    }

    public async Task<List<string>> GetVideoKeysForSectionAsync(Guid sectionId)
    {
        return await _db.Lectures
            .AsNoTracking()
            .Where(l => l.SectionId == sectionId && l.VideoPath != null && l.VideoPath != "")
            .Select(l => l.VideoPath!)
            .ToListAsync();
    }

    // ---------- Counts and positions ----------

    public async Task<int> CountPublishedLecturesAsync(Guid courseId)
    {
        return await _db.Lectures.CountAsync(l => l.IsPublished && l.Section.CourseId == courseId);
    }

    public async Task<int> CountPublishedLecturesInSectionAsync(Guid sectionId)
    {
        return await _db.Lectures.CountAsync(l => l.IsPublished && l.SectionId == sectionId);
    }

    public async Task<int> NextSectionPositionAsync(Guid courseId)
    {
        var max = await _db.Sections
            .Where(s => s.CourseId == courseId)
            .Select(s => (int?)s.Position)
            .MaxAsync();

        return (max ?? 0) + 1;
    }

    public async Task<int> NextLecturePositionAsync(Guid sectionId)
    {
        var max = await _db.Lectures
            .Where(l => l.SectionId == sectionId)
            .Select(l => (int?)l.Position)
            .MaxAsync();

        return (max ?? 0) + 1;
    }

    // ---------- Changes ----------

    public async Task AddSectionAsync(Section section)
    {
        _db.Sections.Add(section);
        await _db.SaveChangesAsync();
    }

    public async Task AddLectureAsync(Lecture lecture)
    {
        _db.Lectures.Add(lecture);
        await _db.SaveChangesAsync();
    }

    // These only mark the item as removed. SaveChangesAsync applies it, together with
    // any renumbering, in one transaction.
    public void RemoveSection(Section section)
    {
        _db.Sections.Remove(section);
    }

    public void RemoveLecture(Lecture lecture)
    {
        _db.Lectures.Remove(lecture);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}