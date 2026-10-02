using Microsoft.AspNetCore.Http;

namespace GvGPoc.Extensions;

public static class HttpContextExtensions
{
    public static string GetRemoteIp(this HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
