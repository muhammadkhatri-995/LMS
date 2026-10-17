using LMS.API.Common;
using LMS.API.Enums;

namespace LMS.API.Interfaces;

public interface IStorageService
{
    // Saves the stream under a new random key and returns the key. `extension` includes the dot, e.g. ".mp4".
    Task<StoredFile> SaveAsync(
        StorageCategory category, Stream content, string extension, CancellationToken cancellationToken = default);

    // Opens a stored file for reading (Module 5 streams videos with this). Null if it does not exist.
    Task<Stream?> OpenReadAsync(StorageCategory category, string key);

    // Deletes a stored file. Returns false if it was missing or could not be deleted.
    Task<bool> DeleteAsync(StorageCategory category, string key);

    // The public address of a file, or null if this kind of file is private.
    string? GetPublicUrl(StorageCategory category, string? key);
}