using System.Text.RegularExpressions;
using LMS.API.Common;
using LMS.API.Enums;
using LMS.API.Interfaces;

namespace LMS.API.Services;

public class LocalStorageService : IStorageService
{
    // The only shape of key we ever create: 32 hex characters, a dot, and a short extension.
    // Checking it on every read and delete means a bad key can never reach outside our folders.
    private static readonly Regex KeyPattern = new(@"^[0-9a-f]{32}\.[a-z0-9]{2,5}$", RegexOptions.Compiled);

    private readonly string _privateRoot;
    private readonly string _thumbnailFolder;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IWebHostEnvironment environment, ILogger<LocalStorageService> logger)
    {
        _logger = logger;

        // Outside wwwroot, so the web server never serves these files directly.
        _privateRoot = Path.Combine(environment.ContentRootPath, "App_Data", "Uploads");

        // Inside wwwroot, on purpose: thumbnails are public pictures.
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        _thumbnailFolder = Path.Combine(webRoot, "uploads", "thumbnails");
    }

    public async Task<StoredFile> SaveAsync(
        StorageCategory category, Stream content, string extension, CancellationToken cancellationToken = default)
    {
        var folder = FolderFor(category);
        Directory.CreateDirectory(folder);

        // The name comes from us, never from the user.
        var key = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();

        if (!KeyPattern.IsMatch(key))
        {
            throw new ArgumentException("Invalid file extension.", nameof(extension));
        }

        var path = Path.Combine(folder, key);

        try
        {
            // CreateNew refuses to overwrite an existing file. The copy streams in small pieces,
            // so even a huge video never sits in memory.
            await using var target = new FileStream(
                path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);

            await content.CopyToAsync(target, 81920, cancellationToken);
        }
        catch
        {
            // Never leave a half-written file behind.
            TryDelete(path);
            throw;
        }

        return new StoredFile(key, new FileInfo(path).Length);
    }

    public Task<Stream?> OpenReadAsync(StorageCategory category, string key)
    {
        var path = PathFor(category, key);

        if (path is null || !File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(StorageCategory category, string key)
    {
        var path = PathFor(category, key);

        if (path is null || !File.Exists(path))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(TryDelete(path));
    }

    public string? GetPublicUrl(StorageCategory category, string? key)
    {
        if (category != StorageCategory.Thumbnail || string.IsNullOrEmpty(key) || !KeyPattern.IsMatch(key))
        {
            return null;
        }

        return "/uploads/thumbnails/" + key;
    }

    // ---------- Helpers ----------

    private string FolderFor(StorageCategory category)
    {
        return category switch
        {
            StorageCategory.Video => Path.Combine(_privateRoot, "Videos"),
            StorageCategory.Assignment => Path.Combine(_privateRoot, "Assignments"),
            StorageCategory.Thumbnail => _thumbnailFolder,
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
    }

    private string? PathFor(StorageCategory category, string key)
    {
        if (string.IsNullOrEmpty(key) || !KeyPattern.IsMatch(key))
        {
            return null;
        }

        return Path.Combine(FolderFor(category), key);
    }

    private bool TryDelete(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The database is already correct. A leftover file is a harmless orphan, so we log and go on.
            _logger.LogWarning(ex, "Could not delete the stored file {FileName}.", Path.GetFileName(path));
            return false;
        }
    }
}