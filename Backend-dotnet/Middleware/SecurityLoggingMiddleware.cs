using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NotepadPlusApi.Middleware;

public class SecurityLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityLoggingMiddleware> _logger;

    public SecurityLoggingMiddleware(RequestDelegate next, ILogger<SecurityLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.Request.Path;
            var method = context.Request.Method;

            _logger.LogInformation(
                "Security: Request from IP {IpAddress} - {Method} {Path}",
                ipAddress, method, path);

            await _next(context);

            if (context.Response.StatusCode >= 400)
            {
                _logger.LogWarning(
                    "Security: Failed request from IP {IpAddress} - {Method} {Path} - Status {StatusCode}",
                    ipAddress, method, path, context.Response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security: Exception during request processing");
            throw;
        }
    }
} 