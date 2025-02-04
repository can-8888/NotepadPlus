using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using NotepadPlusApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace NotepadPlusApi.Auth;

public class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApiKeyAuthHandler> _logger;

    public ApiKeyAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApplicationDbContext context)
        : base(options, logger, encoder)
    {
        _context = context;
        _logger = logger.CreateLogger<ApiKeyAuthHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            _logger.LogInformation("Processing authentication request");
            
            // Skip authentication for login/register endpoints
            if (Request.Path.StartsWithSegments("/api/auth"))
            {
                return AuthenticateResult.NoResult();
            }

            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                _logger.LogWarning("Missing Authorization Header");
                return AuthenticateResult.Fail("Missing Authorization Header");
            }

            var token = authHeader.ToString().Replace("Bearer ", "");
            var userId = Request.Headers["UserId"].FirstOrDefault();

            if (!int.TryParse(userId, out var parsedUserId))
            {
                _logger.LogWarning("Invalid UserId format");
                return AuthenticateResult.Fail("Invalid UserId format");
            }

            _logger.LogInformation("Auth attempt with token: {tokenPresent}, userId: {userId}", 
                !string.IsNullOrEmpty(token), userId);

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Invalid token or missing user ID");
                return AuthenticateResult.Fail("Invalid token or missing user ID");
            }

            // Validate the token in the database
            var userToken = await _context.UserTokens
                .FirstOrDefaultAsync(t => 
                    t.Token == token && 
                    t.UserId == parsedUserId &&
                    t.ExpiresAt > DateTime.UtcNow);

            if (userToken == null)
            {
                return AuthenticateResult.Fail("Invalid or expired token");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("id", userId)  // Add this claim explicitly
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            _logger.LogInformation("Authentication successful for user {userId}", userId);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed");
            return AuthenticateResult.Fail($"Authentication failed: {ex.Message}");
        }
    }
} 