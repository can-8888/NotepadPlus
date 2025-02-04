using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotepadPlusApi.Data;
using NotepadPlusApi.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace NotepadPlusApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ApplicationDbContext context, ILogger<AuthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation($"Login attempt for: {request.Username}");

        // Try to find user by either username or email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                u.Username == request.Username || 
                u.Email == request.Username);

        if (user == null)
        {
            _logger.LogWarning($"User not found: {request.Username}");
            return Unauthorized(new { message = "Invalid username/email or password" });
        }

        if (!VerifyPasswordHash(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password for user: {Username}", user.Username);
            return Unauthorized(new { message = "Invalid username/email or password" });
        }

        // Generate a simple token
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        // Store the token
        var userToken = new UserToken
        {
            UserId = user.Id,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        _context.UserTokens.Add(userToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Login successful for user: {Username}", request.Username);

        return Ok(new LoginResponse
        {
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Name = user.Name,
                CreatedAt = user.CreatedAt
            },
            Token = token
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest request)
    {
        try 
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new { message = "Username already exists" });
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "Email already registered" });
            }

            var hashedPassword = HashPassword(request.Password);
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                Name = request.Name,
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate token for automatic login
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var userToken = new UserToken
            {
                UserId = user.Id,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            _context.UserTokens.Add(userToken);
            await _context.SaveChangesAsync();

            return Ok(new LoginResponse
            {
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Name = user.Name,
                    CreatedAt = user.CreatedAt
                },
                Token = token
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed");
            return StatusCode(500, new { message = "Registration failed" });
        }
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPasswordHash(string password, string storedHash)
    {
        using var sha256 = SHA256.Create();
        var inputHash = HashPassword(password);
        return inputHash == storedHash;
    }
}