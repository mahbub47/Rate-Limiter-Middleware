using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RateLimiterMiddleware.Config;
using RateLimiterMiddleware.Models;
using StackExchange.Redis;

namespace RateLimiterMiddleware.Services
{
    public class FixedWindowRateLimiterService : IRateLimiterService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly RateLimiterConfig _config;
        public FixedWindowRateLimiterService(IConnectionMultiplexer cm, IOptions<RateLimiterConfig> config)
        {
            _connectionMultiplexer = cm;
            _config = config.Value;
        }
        public async Task<RateLimitResult> LimitRate(string id, EndpointRule endpointRule)
        {
            var key = $"ratelimiter:{id}.{endpointRule.Endpoint}";

            var db = _connectionMultiplexer.GetDatabase();
            var currentCount = await db.StringIncrementAsync(key);
            if (currentCount == 1)
                await db.KeyExpireAsync(key, TimeSpan.FromSeconds(endpointRule.WindowSeconds));
            var ttl = await db.KeyTimeToLiveAsync(key);

            return new RateLimitResult()
            {
                IsAllowed = (int)currentCount <= endpointRule.Limit,
                CurrentCount = (int)currentCount,
                Limit = endpointRule.Limit,
                Remaining = (endpointRule.Limit - (int)currentCount) > 0 ? (endpointRule.Limit - (int)currentCount) : 0,
                RetryAfter = (int)currentCount <= endpointRule.Limit ? 0 : (int)ttl.Value.TotalSeconds
            };
        }
    }
}
