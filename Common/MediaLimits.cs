namespace LMS.API.Common;

public static class MediaLimits
{
    public const long MaxVideoBytes = 500L * 1024 * 1024;
    public const long MaxThumbnailBytes = 2L * 1024 * 1024;

    // Extra room for the multipart envelope around the file itself.
    public const long MultipartOverheadBytes = 1024 * 1024;
}