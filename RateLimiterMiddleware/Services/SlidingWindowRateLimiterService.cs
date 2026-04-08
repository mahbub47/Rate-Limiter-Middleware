using Microsoft.Extensions.Options;
using RateLimiterMiddleware.Config;
using RateLimiterMiddleware.Models;
using StackExchange.Redis;

namespace RateLimiterMiddleware.Services
{
    public class SlidingWindowRateLimiterService : IRateLimiterService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly RateLimiterConfig _config;

        public SlidingWindowRateLimiterService(IConnectionMultiplexer connectionMultiplexer, IOptions<RateLimiterConfig> config)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _config = config.Value;
        }
        public async Task<RateLimitResult> LimitRate(string id, EndpointRule endpointRule)
        {
            var key = $"ratelimiter:{id}.{endpointRule.Endpoint}";
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var cutoff = now - (endpointRule.WindowSeconds * 1000);
            var member = $"{now}-{Guid.NewGuid()}";

            var db = _connectionMultiplexer.GetDatabase();

            var removed = await db.SortedSetRemoveRangeByScoreAsync(key, 0, cutoff);
            await db.SortedSetAddAsync(key, member, now);
            var count = await db.SortedSetLengthAsync(key);
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(endpointRule.WindowSeconds));
            var ttl = await db.KeyTimeToLiveAsync(key);
            return new RateLimitResult()
            {
                IsAllowed = (int)count <= endpointRule.Limit,
                CurrentCount = (int)count,
                Limit = endpointRule.Limit,
                Remaining = (endpointRule.Limit - (int)count) > 0 ? (endpointRule.Limit - (int)count) : 0,
                RetryAfter = (int)count <= endpointRule.Limit ? 0 : (int)ttl.Value.TotalSeconds
            };
        }
    }
}
