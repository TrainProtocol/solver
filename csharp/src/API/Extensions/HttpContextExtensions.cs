namespace Train.Solver.API.Extensions;

public static class HttpContextExtensions
{
    public static string GetIpAddress(this HttpContext httpContext)
    {
        if (!string.IsNullOrEmpty(httpContext.Request.Headers["CF-CONNECTING-IP"]))
            return httpContext.Request.Headers["CF-CONNECTING-IP"];

        var ipAddress = httpContext.GetServerVariable("HTTP_X_FORWARDED_FOR");

        if (!string.IsNullOrEmpty(ipAddress))
        {
            var addresses = ipAddress.Split(',');
            if (addresses.Length != 0)
                return addresses.Last();
        }

        return httpContext.Connection.RemoteIpAddress.ToString();
    }
}
