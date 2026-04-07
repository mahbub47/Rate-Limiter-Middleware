namespace RateLimiterMiddleware.Config
{
    public class EndpointRule
    {
        public string Endpoint { get; set; } = "";
        public int Limit { get; set; }
        public int WindowSeconds { get; set; }
    }
}
