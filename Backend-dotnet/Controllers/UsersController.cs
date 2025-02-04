using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotepadPlusApi.Data;
using NotepadPlusApi.Models;
using NotepadPlusApi.Services.Interfaces;

namespace NotepadPlusApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;
    private readonly IUserService _userService;

    public UsersController(ApplicationDbContext context, ILogger<UsersController> logger, IUserService userService)
    {
        _context = context;
        _logger = logger;
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Users controller is working" });
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserDto>>> SearchUsers([FromQuery] string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Ok(new { users = new List<UserDto>() });
            }

            var users = await _userService.SearchUsersAsync(term);
            return Ok(new { users });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return StatusCode(500, new { message = "Error searching users" });
        }
    }

    [HttpGet("ping")]
    public ActionResult<string> Ping()
    {
        _logger.LogInformation("Ping endpoint called");
        return Ok("Users controller is responding");
    }

    [HttpGet("test")]
    public ActionResult TestEndpoint()
    {
        return Ok(new { message = "Users controller is working" });
    }
} 