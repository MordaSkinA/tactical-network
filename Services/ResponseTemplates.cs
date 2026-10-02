using System.Net;
using System.Text.Json;

namespace GvGPoc.Services;

public static class ResponseTemplates
{
    private const string ResultPageTemplate =
        "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><title>Discord link</title>" +
        "<style>body{{background:#111;color:#eee;font-family:system-ui,sans-serif;padding:60px 20px;text-align:center;}}" +
        ".msg{{color:{0};}}</style></head><body><p class=\"msg\">{1}</p>" +
        "<p style=\"color:#888;font-size:13px;\">Redirecting...</p>" +
        "<script>setTimeout(() => location.href = '/menu.html', 2000);</script></body></html>";

    private const string LoginSuccessTemplate =
        "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><title>Discord login</title>" +
        "<style>body{{background:#111;color:#eee;font-family:system-ui,sans-serif;padding:60px 20px;text-align:center;}}</style></head><body>" +
        "<p style=\"color:#4caf50;\">Logged in, redirecting...</p>" +
        "<script>localStorage.setItem('gvg_session', JSON.parse({0})); location.href = '/menu.html';</script>" +
        "</body></html>";

    public static string DiscordResultPage(bool success, string message)
    {
        var color = success ? "#4caf50" : "#e33";
        return string.Format(ResultPageTemplate, color, WebUtility.HtmlEncode(message));
    }

    public static string DiscordLoginSuccessPage(string jsonPayload)
    {
        var safeLiteral = JsonSerializer.Serialize(jsonPayload);
        return string.Format(LoginSuccessTemplate, safeLiteral);
    }
}
