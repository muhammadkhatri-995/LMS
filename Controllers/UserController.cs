using LMS.API.DTOs;
using LMS.API.Enums;
using LMS.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers;

[Authorize]
[Route("api/users")]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // ---------- Any logged-in user ----------

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _userService.GetProfileAsync(CurrentUserId);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var result = await _userService.UpdateProfileAsync(CurrentUserId, request);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var result = await _userService.ChangePasswordAsync(CurrentUserId, request);

        if (!result.Success)
        {
            return ToErrorResponse(result);
        }

        return Ok(new { message = "Password changed successfully." });
    }

    // ---------- Admin only ----------

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] UserRole? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _userService.GetUsersAsync(search, role, page, pageSize);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request)
    {
        var result = await _userService.CreateUserAsync(request);

        if (!result.Success)
        {
            return ToErrorResponse(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetUserStatusRequestDto request)
    {
        var result = await _userService.SetStatusAsync(id, request.IsActive!.Value, CurrentUserId);
        return result.Success ? Ok(result.Data) : ToErrorResponse(result);
    }
}