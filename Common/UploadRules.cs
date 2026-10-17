using Microsoft.AspNetCore.Http;

namespace LMS.API.Common;

public enum UploadKind
{
    Video,
    Thumbnail
}

public static class UploadRules
{
    private static readonly HashSet<string> VideoExtensions = new() { ".mp4", ".webm" };
    private static readonly HashSet<string> ThumbnailExtensions = new() { ".jpg", ".jpeg", ".png", ".webp" };

    private static ReadOnlySpan<byte> PngSignature => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static ReadOnlySpan<byte> WebmSignature => new byte[] { 0x1A, 0x45, 0xDF, 0xA3 };

    // Returns the safe extension to store, or an error message. Never trusts the file name or Content-Type.
    public static (string? Extension, string? Error) Validate(IFormFile? file, UploadKind kind)
    {
        if (file is null || file.Length == 0)
        {
            return (null, "Please choose a file to upload.");
        }

        var maxBytes = kind == UploadKind.Video ? MediaLimits.MaxVideoBytes : MediaLimits.MaxThumbnailBytes;

        if (file.Length > maxBytes)
        {
            return (null, "The file is too large. The maximum size is " + (maxBytes / (1024 * 1024)) + " MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = kind == UploadKind.Video ? VideoExtensions : ThumbnailExtensions;

        if (!allowed.Contains(extension))
        {
            return (null, kind == UploadKind.Video
                ? "Only MP4 and WebM videos are allowed."
                : "Only JPG, PNG, and WebP images are allowed.");
        }

        using var stream = file.OpenReadStream();

        if (!SignatureMatches(extension, stream))
        {
            return (null, "The file content does not match its type.");
        }

        return (extension, null);
    }

    // Reads the first 12 bytes and compares them with what each file type must start with.
    private static bool SignatureMatches(string extension, Stream stream)
    {
        Span<byte> header = stackalloc byte[12];
        var total = 0;

        while (total < header.Length)
        {
            var read = stream.Read(header[total..]);

            if (read == 0)
            {
                break;
            }

            total += read;
        }

        if (total < header.Length)
        {
            return false;
        }

        return extension switch
        {
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header[..8].SequenceEqual(PngSignature),
            ".webp" => header[..4].SequenceEqual("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8),
            ".mp4" => header.Slice(4, 4).SequenceEqual("ftyp"u8),
            ".webm" => header[..4].SequenceEqual(WebmSignature),
            _ => false
        };
    }
}