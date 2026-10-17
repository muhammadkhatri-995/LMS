using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LMS.API.Common;
using LMS.API.Interfaces;
using Microsoft.Extensions.Options;

namespace LMS.API.Services;

public class StreamTicketService : IStreamTicketService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly byte[] _key;

    public StreamTicketService(IOptions<JwtSettings> options)
    {
        // A separate key just for tickets, made from the JWT key. A ticket can never pass as a
        // login token, and a login token can never pass as a ticket.
        var jwtKey = Encoding.UTF8.GetBytes(options.Value.Key);
        _key = HMACSHA256.HashData(jwtKey, Encoding.UTF8.GetBytes("lms-stream-ticket-v1"));
    }

    public (string Ticket, DateTime ExpiresAtUtc) Create(Guid lectureId, Guid userId)
    {
        var expiresAt = DateTime.UtcNow.Add(Lifetime);
        var expiresUnix = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();

        // Payload: lectureId.userId.expiry
        var payload = Encoding.UTF8.GetBytes(
            lectureId.ToString("N") + "." + userId.ToString("N") + "." + expiresUnix.ToString(CultureInfo.InvariantCulture));

        var signature = HMACSHA256.HashData(_key, payload);

        return (ToBase64Url(payload) + "." + ToBase64Url(signature), expiresAt);
    }

    public bool TryValidate(string? ticket, Guid lectureId, out Guid userId)
    {
        userId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(ticket) || ticket.Length > 400)
        {
            return false;
        }

        var parts = ticket.Split('.');

        if (parts.Length != 2)
        {
            return false;
        }

        byte[] payload;
        byte[] signature;

        try
        {
            payload = FromBase64Url(parts[0]);
            signature = FromBase64Url(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        // Compared in constant time, so the timing cannot reveal how many bytes matched.
        var expected = HMACSHA256.HashData(_key, payload);

        if (!CryptographicOperations.FixedTimeEquals(expected, signature))
        {
            return false;
        }

        var fields = Encoding.UTF8.GetString(payload).Split('.');

        if (fields.Length != 3)
        {
            return false;
        }

        if (!Guid.TryParseExact(fields[0], "N", out var ticketLectureId) || ticketLectureId != lectureId)
        {
            return false;
        }

        if (!Guid.TryParseExact(fields[1], "N", out var ticketUserId))
        {
            return false;
        }

        if (!long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var expiresUnix))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expiresUnix)
        {
            return false;
        }

        userId = ticketUserId;
        return true;
    }

    // URL-safe Base64: "+" becomes "-", "/" becomes "_", and the "=" padding is dropped.
    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] FromBase64Url(string text)
    {
        var base64 = text.Replace('-', '+').Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
            case 1: throw new FormatException("Invalid Base64Url length.");
        }

        return Convert.FromBase64String(base64);
    }
}