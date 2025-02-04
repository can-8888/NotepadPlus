using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NotepadPlusApi.Configuration;

namespace NotepadPlusApi.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitPolicy _options;
    private static readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();

    public RateLimitingMiddleware(RequestDelegate next, IOptions<SecurityConfiguration> options)
    {
        _next = next;
        _options = options.Value.RateLimit;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var bucket = _buckets.GetOrAdd(ipAddress, _ => new TokenBucket(_options.MaxRequests, _options.WindowMs));

        if (!bucket.TryTake())
        {
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsJsonAsync(new { message = "Rate limit exceeded" });
            return;
        }

        await _next(context);
    }

    private class TokenBucket
    {
        private readonly int _capacity;
        private readonly double _refillRate;
        private double _tokens;
        private long _lastRefill;

        public TokenBucket(int capacity, int windowMs)
        {
            _capacity = capacity;
            _refillRate = (double)capacity / windowMs;
            _tokens = capacity;
            _lastRefill = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public bool TryTake()
        {
            RefillTokens();
            if (_tokens >= 1)
            {
                _tokens--;
                return true;
            }
            return false;
        }

        private void RefillTokens()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var elapsedMs = now - _lastRefill;
            var tokensToAdd = elapsedMs * _refillRate;
            
            _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
            _lastRefill = now;
        }
    }
} 