using System.Globalization;

namespace LMS.API.Common;

public enum RangeKind
{
    None,            // no usable Range header: send the whole file (200)
    Satisfiable,     // send just the requested slice (206)
    NotSatisfiable   // the slice is outside the file (416)
}

public readonly record struct RangeResult(RangeKind Kind, long Start, long End);

public static class RangeParser
{
    private const string Prefix = "bytes=";

    // Understands:  bytes=0-99   bytes=100-   bytes=-500
    // Anything we do not support or understand is ignored (None), which means "send the whole file".
    public static RangeResult Parse(string? header, long fileLength)
    {
        var none = new RangeResult(RangeKind.None, 0, 0);
        var notSatisfiable = new RangeResult(RangeKind.NotSatisfiable, 0, 0);

        if (string.IsNullOrWhiteSpace(header) || fileLength <= 0)
        {
            return none;
        }

        header = header.Trim();

        if (!header.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return none;
        }

        var spec = header[Prefix.Length..].Trim();

        // Several ranges at once (bytes=0-9,20-29) are not supported. Browsers do not use them for video.
        if (spec.Contains(','))
        {
            return none;
        }

        var dash = spec.IndexOf('-');

        if (dash < 0)
        {
            return none;
        }

        var startText = spec[..dash].Trim();
        var endText = spec[(dash + 1)..].Trim();

        // "bytes=-500" means "the last 500 bytes".
        if (startText.Length == 0)
        {
            if (!TryParseNumber(endText, out var suffixLength))
            {
                return none;
            }

            if (suffixLength == 0)
            {
                return notSatisfiable;
            }

            var suffixStart = Math.Max(fileLength - suffixLength, 0);
            return new RangeResult(RangeKind.Satisfiable, suffixStart, fileLength - 1);
        }

        if (!TryParseNumber(startText, out var start))
        {
            return none;
        }

        long end;

        if (endText.Length == 0)
        {
            // "bytes=100-" means "from byte 100 to the end".
            end = fileLength - 1;
        }
        else
        {
            if (!TryParseNumber(endText, out var requestedEnd) || requestedEnd < start)
            {
                return none;
            }

            // An end past the last byte is allowed. We simply stop at the last byte.
            end = Math.Min(requestedEnd, fileLength - 1);
        }

        if (start >= fileLength)
        {
            return notSatisfiable;
        }

        return new RangeResult(RangeKind.Satisfiable, start, end);
    }

    // NumberStyles.None: digits only, so no signs, spaces, or "1e3".
    private static bool TryParseNumber(string text, out long value)
    {
        return long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
}