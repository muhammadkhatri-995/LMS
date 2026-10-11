using System.Security.Claims;
using LMS.API.Common;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    protected IActionResult ToErrorResponse<T>(ServiceResult<T> result)
    {
        var body = new { error = result.Error };

        return result.ErrorType switch
        {
            ServiceErrorType.Validation => BadRequest(body),
            ServiceErrorType.Unauthorized => Unauthorized(body),
            ServiceErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, body),
            ServiceErrorType.NotFound => NotFound(body),
            _ => Conflict(body)
        };
    }
}