namespace RateLimiterMiddleware.Models
{
    public class RateLimitResult
    {
        public bool IsAllowed { get; set; }
        public int CurrentCount { get; set; }
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public int RetryAfter { get; set; }
    }
}
