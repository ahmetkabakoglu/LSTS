using Lsts.Api.Infrastructure;
using Oracle.ManagedDataAccess.Client;

namespace Lsts.Api.Endpoints;

public static class DbPingEndpoints
{
    public static WebApplication MapDbPingEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.MapGet("/api/_db/ping", async (DbFactory db) =>
        {
            try
            {
                await using var conn = await db.OpenConnectionAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1 FROM dual";

                var result = await cmd.ExecuteScalarAsync();
                return Results.Ok(new { ok = true, db = "oracle", result });
            }
            catch (OracleException ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .RequireAuthorization("DevTools");

        return app;
    }
}