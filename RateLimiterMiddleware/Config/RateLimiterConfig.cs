namespace RateLimiterMiddleware.Config
{
    public class RateLimiterConfig
    {
        public int DefaultLimit { get; set; }
        public int DefaultWindowSeconds { get; set; }
        public List<EndpointRule> EndpointRules { get; set; } = new();
    }
}
