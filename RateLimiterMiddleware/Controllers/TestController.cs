using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RateLimiterMiddleware.Attributes;
using RateLimiterMiddleware.Config;
using RateLimiterMiddleware.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace RateLimiterMiddleware.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    public class TestController : ControllerBase
    {
        [RateLimiter(limit: 20, windowSeconds: 30)]
        [HttpGet("data")]
        public IActionResult GetData()
        {
            return Ok("Data");
        }
    }
}
