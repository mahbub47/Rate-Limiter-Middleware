using Microsoft.Extensions.Options;
using RateLimiterMiddleware.Attributes;
using RateLimiterMiddleware.Config;
using RateLimiterMiddleware.Identity;
using RateLimiterMiddleware.Services;

namespace RateLimiterMiddleware.Middlewares
{
    public class RateLimiterMiddleware
    {
        private readonly RequestDelegate _next;

        public RateLimiterMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context, 
            IRateLimiterService service, 
            IOptions<RateLimiterConfig> config,
            IClientIdentityResolver identityResolver)
        {
            var id = identityResolver.Resolve(context);
            var path = context.Request.Path.Value ?? "/";

            var endpoint = context.GetEndpoint();
            var rule = new EndpointRule();

            if(endpoint != null)
            {
                var attribute = endpoint.Metadata.GetMetadata<RateLimiterAttribute>();
                rule = ResolveEndpoint(attribute, path, config.Value);
            }
            else
            {
                await _next(context);
            }

            var result = await service.LimitRate(id, rule);

            if (!result.IsAllowed)
            {
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
                context.Response.Headers["X-RetryAfter"] = result.RetryAfter.ToString();
                context?.Response.WriteAsJsonAsync(new
                {
                    error = "Too Many Requests",
                    message = "You are sending too many request",
                    retryAfter = result.RetryAfter,
                });
                return;
            }

            context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
            await _next(context);
        }

        private EndpointRule ResolveEndpoint(RateLimiterAttribute? attribute, string path, RateLimiterConfig config)
        {
            if (attribute != null)
                return new EndpointRule
                {
                    Endpoint = path,
                    Limit = attribute.Limit,
                    WindowSeconds = attribute.WindowSeconds,
                };
            return config.EndpointRules.FirstOrDefault(er => er.Endpoint == path) ??
                new EndpointRule
                {
                    Endpoint = path,
                    Limit = config.DefaultLimit,
                    WindowSeconds = config.DefaultWindowSeconds,
                };
        }
    }
}
