using LMS.API.Common;
using LMS.API.DTOs;

namespace LMS.API.Interfaces;

public interface IStreamingService
{
    // Called with the normal login token: may this user watch the lecture? If so, here is a ticket.
    Task<ServiceResult<StreamTicketResponseDto>> CreateTicketAsync(Guid lectureId, Guid userId);

    // Called by the <video> tag with only the ticket. Re-checks everything, then opens the file.
    Task<ServiceResult<VideoSource>> OpenStreamAsync(Guid lectureId, string? ticket);
}