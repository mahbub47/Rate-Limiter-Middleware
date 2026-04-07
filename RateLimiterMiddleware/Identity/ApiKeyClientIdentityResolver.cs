using System.Reflection.Metadata.Ecma335;

namespace RateLimiterMiddleware.Identity
{
    public class ApiKeyClientIdentityResolver : IClientIdentityResolver
    {
        public string Resolve(HttpContext context)
        {
            return context.Request.Headers["X-Api-Key"].ToString() ?? 
                context.Connection.RemoteIpAddress?.ToString() ??
                "unknown";
        }
    }
}
