using System.Collections.Generic;

namespace NotepadPlusApi.Configuration;

public class SecurityConfiguration
{
    public Dictionary<string, string> SecurityHeaders { get; set; } = new();
    public CorsPolicy CorsPolicy { get; set; } = new();
    public RateLimitPolicy RateLimit { get; set; } = new();
}

public class CorsPolicy
{
    public string[] AllowedOrigins { get; set; } = new[] { "http://localhost:3000" };
    public string[] AllowedMethods { get; set; } = new[] { "GET", "POST", "PUT", "DELETE" };
    public string[] AllowedHeaders { get; set; } = new[] { "Content-Type", "Authorization" };
    public bool AllowCredentials { get; set; } = true;
    public int MaxAge { get; set; } = 86400;
}

public class RateLimitPolicy
{
    public int WindowMs { get; set; } = 900000; // 15 minutes
    public int MaxRequests { get; set; } = 100;
} 