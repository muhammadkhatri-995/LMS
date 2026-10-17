using System.Buffers;
using Microsoft.AspNetCore.Http;

namespace LMS.API.Common;

public static class RangeResponse
{
    private const int BufferSize = 64 * 1024;

    // Writes either the whole file (200) or one slice of it (206), straight to the response.
    // The file is read in 64 KB pieces, so memory use does not depend on the file size.
    public static async Task WriteAsync(
        HttpContext context, Stream content, string contentType, CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
        {
            throw new NotSupportedException("Streaming needs a seekable stream.");
        }

        var response = context.Response;
        var length = content.Length;

        response.ContentType = contentType;
        response.Headers.AcceptRanges = "bytes";
        response.Headers.CacheControl = "private, no-cache";

        var range = RangeParser.Parse(context.Request.Headers.Range.ToString(), length);

        if (range.Kind == RangeKind.NotSatisfiable)
        {
            response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
            response.Headers.ContentRange = "bytes */" + length;
            return;
        }

        long start = 0;
        long count = length;

        if (range.Kind == RangeKind.Satisfiable)
        {
            start = range.Start;
            count = range.End - range.Start + 1;

            response.StatusCode = StatusCodes.Status206PartialContent;
            response.Headers.ContentRange = "bytes " + range.Start + "-" + range.End + "/" + length;
        }
        else
        {
            response.StatusCode = StatusCodes.Status200OK;
        }

        response.ContentLength = count;

        await CopyAsync(content, response.Body, start, count, cancellationToken);
    }

    private static async Task CopyAsync(
        Stream source, Stream destination, long start, long count, CancellationToken cancellationToken)
    {
        if (start > 0)
        {
            source.Seek(start, SeekOrigin.Begin);
        }

        // A reusable buffer from a shared pool, so we do not create garbage for every request.
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

        try
        {
            var remaining = count;

            while (remaining > 0)
            {
                var toRead = (int)Math.Min(buffer.Length, remaining);
                var read = await source.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken);

                if (read == 0)
                {
                    break;
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                remaining -= read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}