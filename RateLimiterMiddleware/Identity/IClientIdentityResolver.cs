namespace RateLimiterMiddleware.Identity
{
    public interface IClientIdentityResolver
    {
        string Resolve(HttpContext context);
    }
}
