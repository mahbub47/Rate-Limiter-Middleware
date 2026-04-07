namespace RateLimiterMiddleware.Identity
{
    public class IpClientIdentityResolver : IClientIdentityResolver
    {
        public string Resolve(HttpContext context)
        {
            return context.Request.Headers["X-Forwarded-For"].ToString() ?? 
                context.Connection.RemoteIpAddress?.ToString() ??
                "unknown";
        }
    }
}
