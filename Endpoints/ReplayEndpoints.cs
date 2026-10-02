using GvGPoc.Models;
using GvGPoc.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GvGPoc.Endpoints;

public static class ReplayEndpoints
{
    public static void MapReplayEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{fileName}/download", async (
            string fileName,
            string? access_token,
            ISessionStore sessions,
            IBattleState state) => {

            var session = access_token is not null ? sessions.Get(access_token) : null;
            if (session is null) return Results.Unauthorized();
            if (session.Role is not (UserRole.Commander or UserRole.Admin or UserRole.Developer))
                return Results.Forbid();

            var safeName = Path.GetFileName(fileName);
            var bytes = await state.ReadLogFileBytesAsync(safeName);
            if (bytes is null) return Results.NotFound();

            return Results.File(bytes, "application/json", fileDownloadName: safeName);
        });
    }
}
