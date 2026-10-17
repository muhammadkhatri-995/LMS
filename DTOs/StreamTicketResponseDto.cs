namespace LMS.API.DTOs;

public class StreamTicketResponseDto
{
    // A relative address the <video> tag can use, e.g. /api/lectures/{id}/stream?ticket=...
    public string Url { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}