using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace NotepadPlusApi.Middleware;

public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _secretKey;
    private readonly ILogger<JwtValidationMiddleware> _logger;

    public JwtValidationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _secretKey = configuration["AppSettings:Token"] ?? throw new InvalidOperationException("JWT Token secret is not configured");
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                context.Items["UserId"] = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JWT validation failed");
            }
        }

        await _next(context);
    }
} 