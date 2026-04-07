namespace RateLimiterMiddleware.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RateLimiterAttribute : Attribute
    {
        public int Limit { get; set; }
        public int WindowSeconds { get; set; }

        public RateLimiterAttribute(int limit, int windowSeconds)
        {
            Limit = limit;
            WindowSeconds = windowSeconds;
        }
    }
}
