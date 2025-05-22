using Microsoft.AspNetCore.Http;

namespace Train.Solver.Util.Extensions;

public static class HttpContextExtensions
{
    public static string GetIpAddress(this HttpContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        // Cloudflare
        if (context.Request.Headers.TryGetValue("CF-CONNECTING-IP", out var cfIp) && !string.IsNullOrWhiteSpace(cfIp))
            return cfIp.ToString();

        // Common proxy header
        if (context.Request.Headers.TryGetValue("X-FORWARDED-FOR", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ip = forwardedFor.ToString().Split(',').LastOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        // Direct connection IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
