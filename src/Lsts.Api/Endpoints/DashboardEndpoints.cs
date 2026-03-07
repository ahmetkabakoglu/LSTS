using Lsts.Api.Infrastructure;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Lsts.Api.Endpoints;

public static class DashboardEndpoints
{
    public static WebApplication MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard/summary", async (
            int? recent,
            int? activity,
            DbFactory db) =>
        {
            var takeRecent = Math.Clamp(recent ?? 10, 1, 50);
            var takeAct = Math.Clamp(activity ?? 10, 1, 50);

            await using var conn = await db.OpenConnectionAsync();

            var kpis = new
            {
                openTickets = 0,
                waitingParts = 0,
                inRepair = 0,
                closedToday = 0
            };

            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.CommandText = @"
                    SELECT
                    SUM(CASE WHEN current_status NOT IN ('CLOSED','CANCELED') THEN 1 ELSE 0 END) AS open_tickets,
                    SUM(CASE WHEN current_status = 'WAITING_PARTS' THEN 1 ELSE 0 END) AS waiting_parts,
                    SUM(CASE WHEN current_status = 'IN_REPAIR' THEN 1 ELSE 0 END) AS in_repair,
                    SUM(CASE WHEN current_status = 'CLOSED'
                            AND TRUNC(CAST(NVL(status_updated_at, created_at) AS DATE)) = TRUNC(SYSDATE)
                        THEN 1 ELSE 0 END) AS closed_today
                    FROM LSTS_TICKETS";

                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    kpis = new
                    {
                        openTickets = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0)),
                        waitingParts = r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetValue(1)),
                        inRepair = r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2)),
                        closedToday = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3)),
                    };
                }
            }

            var recentTickets = new List<object>();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;
                cmd.CommandText = $@"
SELECT
  ticket_id,
  service_no,
  current_status,
  fault_desc,
  NVL(status_updated_at, created_at) AS at
FROM LSTS_TICKETS
ORDER BY NVL(status_updated_at, created_at) DESC, ticket_id DESC
FETCH FIRST {takeRecent} ROWS ONLY";

                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    recentTickets.Add(new
                    {
                        ticketId = Convert.ToInt64(r.GetValue(0)),
                        serviceNo = r.IsDBNull(1) ? "" : r.GetString(1),
                        status = r.IsDBNull(2) ? "" : r.GetString(2),
                        fault = r.IsDBNull(3) ? "" : r.GetString(3),
                        at = r.IsDBNull(4) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(4)
                    });
                }
            }

            var activityItems = new List<object>();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.BindByName = true;

                cmd.CommandText = $@"
                    SELECT icon, text, at
                    FROM (
                    SELECT
                        'mdi-tools' AS icon,
                        'Ticket #' || TO_CHAR(ticket_id) || ' status ' || NVL(current_status,'') AS text,
                        NVL(status_updated_at, created_at) AS at
                    FROM LSTS_TICKETS

                    UNION ALL

                    SELECT
                        'mdi-clipboard-list-outline' AS icon,
                        'Part request #' || TO_CHAR(request_id) || ' ' || NVL(request_status,'') AS text,
                        NVL(requested_at, created_at) AS at
                    FROM LSTS_PART_REQUESTS

                    UNION ALL

                    SELECT
                        'mdi-memory' AS icon,
                        'Part usage: ticket #' || TO_CHAR(ticket_id) || ' qty ' || TO_CHAR(qty) AS text,
                        created_at AS at
                    FROM LSTS_TICKET_PART_USAGE
                    )
                    ORDER BY at DESC
                    FETCH FIRST {takeAct} ROWS ONLY";

                await using var r = await cmd.ExecuteReaderAsync();
                var i = 0;
                while (await r.ReadAsync())
                {
                    i++;
                    activityItems.Add(new
                    {
                        id = i,
                        icon = r.IsDBNull(0) ? "mdi-information-outline" : r.GetString(0),
                        text = r.IsDBNull(1) ? "" : r.GetString(1),
                        at = r.IsDBNull(2) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(2)
                    });
                }
            }

            return Results.Ok(new
            {
                kpis,
                recentTickets,
                activity = activityItems
            });
        })
        .RequireAuthorization("Tickets.Dashboard");

        return app;
    }
}