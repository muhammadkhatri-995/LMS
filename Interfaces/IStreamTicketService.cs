namespace LMS.API.Interfaces;

public interface IStreamTicketService
{
    // Makes a signed ticket tied to one lecture and one user.
    (string Ticket, DateTime ExpiresAtUtc) Create(Guid lectureId, Guid userId);

    // True only if the signature is genuine, the ticket is for this lecture, and it has not expired.
    bool TryValidate(string? ticket, Guid lectureId, out Guid userId);
}