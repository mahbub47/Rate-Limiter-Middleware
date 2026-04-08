# 🚦 Rate Limiter Middleware

A production-ready, Redis-backed **rate limiting middleware** for ASP.NET Core 9 APIs. Built with a clean, extensible architecture that supports per-endpoint rules, attribute-based configuration, and pluggable client identity resolution strategies.

---

## ✨ Features

- **Fixed Window Algorithm** — Efficient request counting with automatic window expiry via Redis TTL
- **Per-Endpoint Rate Limiting** — Define granular limits for individual routes via `appsettings.json`
- **Attribute-Based Override** — Decorate any controller action with `[RateLimiter]` for inline, per-action rules
- **Pluggable Identity Resolution** — Swap between IP-based or API Key-based client identification through a clean `IClientIdentityResolver` abstraction
- **Standard HTTP Response Headers** — Returns `X-RateLimit-Limit`, `X-RateLimit-Remaining`, and `X-RetryAfter` on every response
- **429 Too Many Requests** — Proper RFC-compliant error response with retry metadata in JSON
- **Redis-Backed State** — Horizontally scalable; no in-memory state means it works across multiple API instances
- **Zero Middleware Configuration Boilerplate** — One-line registration in `Program.cs`

---

## 🏗️ Architecture

```
├── Attributes/
│   └── RateLimiterAttribute.cs       # Per-action attribute for inline limit configuration
├── Config/
│   ├── RateLimiterConfig.cs          # Strongly-typed config bound from appsettings.json
│   └── EndpointRule.cs               # Per-endpoint rule model
├── Identity/
│   ├── IClientIdentityResolver.cs    # Abstraction for client identification
│   ├── IpClientIdentityResolver.cs   # Resolves client by IP / X-Forwarded-For
│   └── ApiKeyClientIdentityResolver.cs  # Resolves client by X-Api-Key header
├── Middlewares/
│   └── RateLimiterMiddleware.cs      # Core middleware — intercepts, evaluates, responds
├── Models/
│   └── RateLimitResult.cs            # Result DTO from the rate limiter service
└── Services/
    ├── IRateLimiterService.cs         # Rate limiter service abstraction
    └── FixedWindowRateLimiterService.cs  # Redis fixed-window implementation
```

### How It Works

```
Incoming Request
      │
      ▼
RateLimiterMiddleware
      │
      ├─ Resolve Client Identity (IP or API Key)
      │
      ├─ Resolve Endpoint Rule
      │     ├─ [RateLimiter] attribute on action?  → Use attribute values
      │     ├─ Matching rule in appsettings.json?   → Use endpoint rule
      │     └─ Fallback                             → Use global defaults
      │
      ├─ Call FixedWindowRateLimiterService (Redis INCR + TTL)
      │
      ├─ Allowed? → Set X-RateLimit headers → next()
      └─ Blocked? → 429 with Retry-After JSON body
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- A running Redis instance ([Upstash](https://upstash.com), local Docker, or any Redis-compatible server)

### Installation

```bash
git clone https://github.com/your-username/Rate-Limiter-Middleware.git
cd Rate-Limiter-Middleware
dotnet restore
```

### Configuration

Update `appsettings.json` with your Redis connection string and desired limits:

```json
{
  "ConnectionStrings": {
    "Redis": "your-redis-host:6379,password=your-password,ssl=True,abortConnect=False"
  },
  "RateLimiter": {
    "DefaultLimit": 10,
    "DefaultWindowSeconds": 60,
    "EndpointRules": [
      {
        "Endpoint": "/api/auth/login",
        "Limit": 5,
        "WindowSeconds": 60
      }
    ]
  }
}
```

### Run

```bash
dotnet run --project RateLimiterMiddleware
```

---

## 🔧 Usage

### 1. Register the Middleware (`Program.cs`)

```csharp
// Register Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

// Register the rate limiter service
builder.Services.AddScoped<IRateLimiterService, FixedWindowRateLimiterService>();

// Bind config
builder.Services.Configure<RateLimiterConfig>(
    builder.Configuration.GetSection("RateLimiter"));

// Choose identity resolution strategy
builder.Services.AddTransient<IClientIdentityResolver, IpClientIdentityResolver>();
// or: builder.Services.AddTransient<IClientIdentityResolver, ApiKeyClientIdentityResolver>();

// Add to pipeline
app.UseMiddleware<RateLimiterMiddleware>();
```

### 2. Attribute-Based Limiting on Controller Actions

Override the global/config limits on a per-action basis with the `[RateLimiter]` attribute:

```csharp
[ApiController]
[Route("/api/[controller]")]
public class DataController : ControllerBase
{
    // Custom: 20 requests per 30 seconds for this endpoint only
    [RateLimiter(limit: 20, windowSeconds: 30)]
    [HttpGet("data")]
    public IActionResult GetData()
    {
        return Ok("Data");
    }

    // Falls back to global defaults from appsettings.json
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        return Ok("Info");
    }
}
```

### 3. Switching Identity Resolvers

The `IClientIdentityResolver` interface decouples client identification from rate limit logic. Two implementations are included:

| Resolver | Identifies client by | Use case |
|---|---|---|
| `IpClientIdentityResolver` | `X-Forwarded-For` / Remote IP | Public APIs, general use |
| `ApiKeyClientIdentityResolver` | `X-Api-Key` header | Partner APIs, authenticated clients |

Swap them by changing a single DI registration line in `Program.cs`.

---

## 📡 Response Headers

Every response includes rate limit metadata:

| Header | Description |
|---|---|
| `X-RateLimit-Limit` | Maximum requests allowed in the window |
| `X-RateLimit-Remaining` | Requests remaining in the current window |
| `X-RetryAfter` | Seconds until the window resets (on 429 only) |

### 429 Response Body

```json
{
  "error": "Too Many Requests",
  "message": "You are sending too many requests",
  "retryAfter": 42
}
```

---

## 🔌 Extending the Middleware

### Custom Identity Resolver

Implement `IClientIdentityResolver` to identify clients by JWT claims, tenant ID, or any other signal:

```csharp
public class JwtClientIdentityResolver : IClientIdentityResolver
{
    public string Resolve(HttpContext context)
    {
        return context.User.FindFirst("sub")?.Value ?? "anonymous";
    }
}
```

Register it in `Program.cs`:

```csharp
builder.Services.AddTransient<IClientIdentityResolver, JwtClientIdentityResolver>();
```

### Custom Rate Limiting Algorithm

Implement `IRateLimiterService` to use a sliding window, token bucket, or any other algorithm:

```csharp
public class SlidingWindowRateLimiterService : IRateLimiterService
{
    public Task<RateLimitResult> LimitRate(string id, EndpointRule rule)
    {
        // your implementation
    }
}
```

---

## 🛠️ Tech Stack

| Technology | Role |
|---|---|
| **ASP.NET Core 9** | Web framework & middleware pipeline |
| **StackExchange.Redis 2.12** | Redis client for distributed counter state |
| **Redis** | Fast, atomic request counters with TTL expiry |
| **OpenAPI / Scalar** | API documentation (dev environment) |

---

## 📄 License

This project is open source and available under the [MIT License](LICENSE).