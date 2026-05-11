using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Shortly.Api.RateLimiting;

public static class RateLimitingPolicies
{
    public const string PerClient = "per-client";
    public const string PerIp = "per-ip";
}

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>Maximum requests per window for authenticated API clients.</summary>
    public int ClientPermitLimit { get; set; } = 1000;

    /// <summary>Window duration in seconds for authenticated API clients.</summary>
    public int ClientWindowSeconds { get; set; } = 60;

    /// <summary>Maximum requests per window for anonymous/redirect traffic (per IP).</summary>
    public int IpPermitLimit { get; set; } = 2000;

    /// <summary>Window duration in seconds for anonymous/redirect traffic (per IP).</summary>
    public int IpWindowSeconds { get; set; } = 60;
}

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimitingPolicies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>()
                       ?? new RateLimitSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    """{"error":"Too many requests. Please try again later.","statusCode":429}""",
                    cancellationToken);
            };

            // Per-client policy: keyed by authenticated client_id claim
            options.AddPolicy(RateLimitingPolicies.PerClient, context =>
            {
                var clientId = context.User?.FindFirstValue("client_id") ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(clientId, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.ClientPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.ClientWindowSeconds),
                        QueueLimit = 0
                    });
            });

            // Per-IP policy: keyed by remote IP for anonymous/redirect endpoints
            options.AddPolicy(RateLimitingPolicies.PerIp, context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.IpPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.IpWindowSeconds),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
