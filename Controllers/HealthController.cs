using LMS.API.Data;
using Microsoft.AspNetCore.Mvc;
namespace LMS.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "LMS API is running",
            serverTimeUtc = DateTime.UtcNow
        });
    }
    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase()
    {
        var canConnect = await _db.Database.CanConnectAsync();

        if (!canConnect)
        {
            return StatusCode(503, new { database = "unreachable" });
        }

        return Ok(new { database = "connected" });
    }
}

   