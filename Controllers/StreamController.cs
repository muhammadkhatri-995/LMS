using LMS.API.Common;
using LMS.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers;

[Route("api/lectures")]
public class StreamController : ApiControllerBase
{
    private readonly IStreamingService _streamingService;

    public StreamController(IStreamingService streamingService)
    {
        _streamingService = streamingService;
    }

    // Step 1: the player asks with the normal login token and receives a short-lived ticket URL.
    [Authorize]
    [HttpPost("{id:guid}/stream-ticket")]
    public async Task<IActionResult> CreateTicket(Guid id)
    {
        var result = await _streamingService.CreateTicketAsync(id, CurrentUserId);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    // Step 2: the <video> tag calls this. It cannot send an Authorization header, so the ticket in
    // the URL is the proof. AllowAnonymous only skips the JWT check. The ticket is checked inside.
    [AllowAnonymous]
    [HttpGet("{id:guid}/stream")]
    public async Task<IActionResult> Stream(Guid id, [FromQuery] string? ticket, CancellationToken cancellationToken)
    {
        var result = await _streamingService.OpenStreamAsync(id, ticket);

        if (!result.Success)
        {
            return ToErrorResponse(result);
        }

        await using var content = result.Data!.Content;

        try
        {
            await RangeResponse.WriteAsync(HttpContext, content, result.Data.ContentType, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The viewer closed the tab or jumped somewhere else. Browsers do this constantly.
        }
        catch (IOException)
        {
            // The connection dropped while we were writing. Nothing to fix.
        }

        // The response was written directly above.
        return new EmptyResult();
    }
}