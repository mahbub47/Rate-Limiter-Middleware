using RateLimiterMiddleware.Config;
using RateLimiterMiddleware.Models;

namespace RateLimiterMiddleware.Services
{
    public interface IRateLimiterService
    {
        Task<RateLimitResult> LimitRate(string id, EndpointRule endpointRule);
    }
}
