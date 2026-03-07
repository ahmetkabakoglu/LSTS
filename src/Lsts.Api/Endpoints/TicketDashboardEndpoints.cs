using Lsts.Api.Contracts;
using Lsts.Api.Infrastructure;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using static Lsts.Api.Infrastructure.DbValue;

namespace Lsts.Api.Endpoints;

public static class TicketDashboardEndpoints
{
    public static WebApplication MapTicketDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/tickets/{ticketId:long}/part-usage", async (long ticketId, DbFactory db) =>
        {
            if (ticketId <= 0) return Results.BadRequest(new { error = "ticketId must be > 0" });

            await using var conn = await db.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;

            cmd.CommandText = @"
                SELECT u.usage_id,
                    u.ticket_id,
                    u.part_id,
                    p.part_code,
                    u.qty,
                    u.unit_price,
                    u.line_total,
                    u.note,
                    u.created_at,
                    u.created_by
                FROM LSTS_TICKET_PART_USAGE u
                JOIN LSTS_PARTS p ON p.part_id = u.part_id
                WHERE u.ticket_id = :p_ticket_id
                ORDER BY u.created_at DESC, u.usage_id DESC";

            cmd.Parameters.Add(new OracleParameter("p_ticket_id", OracleDbType.Int64, ticketId, ParameterDirection.Input));

            var rows = new List<object>();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                rows.Add(new
                {
                    usageId = ToInt64(r.GetValue(0)),
                    ticketId = ToInt64(r.GetValue(1)),
                    partId = ToInt64(r.GetValue(2)),
                    partCode = r.GetString(3),
                    qty = ToInt32(r.GetValue(4)),
                    unitPrice = ToDecimal(r.IsDBNull(5) ? 0m : r.GetValue(5)),
                    lineTotal = ToDecimal(r.IsDBNull(6) ? 0m : r.GetValue(6)),
                    note = r.IsDBNull(7) ? null : r.GetString(7),
                    createdAt = r.GetFieldValue<DateTimeOffset>(8),
                    createdBy = ToInt64(r.GetValue(9))
                });
            }

            return Results.Ok(rows);
        })
        .RequireAuthorization("Tickets.Dashboard");

        app.MapGet("/api/tickets/{ticketId:long}/part-usage/summary", async (long ticketId, DbFactory db) =>
        {
            if (ticketId <= 0) return Results.BadRequest(new { error = "ticketId must be > 0" });

            await using var conn = await db.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;

            cmd.CommandText = @"
                SELECT u.part_id,
                       p.part_code,
                       SUM(u.qty) AS total_qty,
                       COUNT(*) AS row_count
                FROM LSTS_TICKET_PART_USAGE u
                JOIN LSTS_PARTS p ON p.part_id = u.part_id
                WHERE u.ticket_id = :p_ticket_id
                GROUP BY u.part_id, p.part_code
                ORDER BY p.part_code";

            cmd.Parameters.Add(new OracleParameter("p_ticket_id", OracleDbType.Int64, ticketId, ParameterDirection.Input));

            var rows = new List<object>();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                rows.Add(new
                {
                    partId = ToInt64(r.GetValue(0)),
                    partCode = r.GetString(1),
                    totalQty = ToInt32(r.GetValue(2)),
                    rowCount = ToInt32(r.GetValue(3))
                });
            }

            return Results.Ok(rows);
        })
        .RequireAuthorization("Tickets.Dashboard");

        app.MapGet("/api/tickets/{ticketId:long}/part-requests", async (long ticketId, DbFactory db) =>
        {
            if (ticketId <= 0) return Results.BadRequest(new { error = "ticketId must be > 0" });

            await using var conn = await db.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;

            cmd.CommandText = @"
                SELECT request_id,
                       ticket_id,
                       request_status,
                       note,
                       requested_by,
                       requested_at,
                       created_by
                FROM LSTS_PART_REQUESTS
                WHERE ticket_id = :p_ticket_id
                ORDER BY request_id DESC";

            cmd.Parameters.Add(new OracleParameter("p_ticket_id", OracleDbType.Int64, ticketId, ParameterDirection.Input));

            var rows = new List<object>();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                rows.Add(new
                {
                    requestId = ToInt64(r.GetValue(0)),
                    ticketId = ToInt64(r.GetValue(1)),
                    requestStatus = r.IsDBNull(2) ? null : r.GetString(2),
                    note = r.IsDBNull(3) ? null : r.GetString(3),
                    requestedBy = ToInt64(r.GetValue(4)),
                    requestedAt = r.GetFieldValue<DateTimeOffset>(5),
                    createdBy = ToInt64(r.GetValue(6))
                });
            }

            return Results.Ok(rows);
        })
        .RequireAuthorization("Tickets.Dashboard");

        app.MapGet("/api/tickets/{ticketId:long}/dashboard", async (
            long ticketId,
            DbFactory db) =>
        {
            if (ticketId <= 0)
                return Results.BadRequest(new { error = "ticketId must be > 0" });

            await using var conn = await db.OpenConnectionAsync();

            TicketDto ticket;
            await using (var tCmd = conn.CreateCommand())
            {
                tCmd.BindByName = true;
                tCmd.CommandText = @"
                    SELECT ticket_id,
                           service_no,
                           device_id,
                           current_status,
                           status_updated_at,
                           fault_desc,
                           internal_notes,
                           public_note,
                           estimated_delivery_date
                    FROM LSTS_TICKETS
                    WHERE ticket_id = :p_ticket_id";
                tCmd.Parameters.Add(new OracleParameter("p_ticket_id", OracleDbType.Int64, ticketId, ParameterDirection.Input));

                await using var tr = await tCmd.ExecuteReaderAsync();
                if (!await tr.ReadAsync())
                    return Results.NotFound(new { error = "Ticket not found" });

                ticket = new TicketDto
                {
                    TicketId = ToInt64(tr.GetValue(0)),
                    ServiceNo = tr.IsDBNull(1) ? "" : tr.GetString(1),
                    DeviceId = ToInt64(tr.GetValue(2)),
                    CurrentStatus = tr.IsDBNull(3) ? "" : tr.GetString(3),
                    StatusUpdatedAt = tr.IsDBNull(4) ? null : tr.GetFieldValue<DateTimeOffset>(4),
                    FaultDesc = tr.IsDBNull(5) ? "" : tr.GetString(5),
                    InternalNotes = tr.IsDBNull(6) ? null : tr.GetString(6),
                    PublicNote = tr.IsDBNull(7) ? null : tr.GetString(7),
                    EstimatedDeliveryDate = tr.IsDBNull(8) ? null : tr.GetDateTime(8)
                };
            }

            var partRequests = new List<PartRequestDto>();

            await using (var prCmd = conn.CreateCommand())
            {
                prCmd.BindByName = true;
                prCmd.CommandText = @"
                    SELECT request_id,
                           request_status,
                           note,
                           requested_by,
                           requested_at,
                           created_by,
                           created_at
                    FROM LSTS_PART_REQUESTS
                    WHERE ticket_id = :p_ticket_id
                    ORDER BY request_id DESC";
                prCmd.Parameters.Add(new OracleParameter("p_ticket_id", OracleDbType.Int64, ticketId, ParameterDirection.Input));

                await using var pr = await prCmd.ExecuteReaderAsync();
                while (await pr.ReadAsync())
                {
                    partRequests.Add(new PartRequestDto
                    {
                        RequestId = ToInt64(pr.GetValue(0)),
                        RequestStatus = pr.IsDBNull(1) ? "" : pr.GetString(1),
                        Note = pr.IsDBNull(2) ? null : pr.GetString(2),
                        RequestedBy = ToInt64(pr.GetValue(3)),
                        RequestedAt = pr.IsDBNull(4) ? null : pr.GetFieldValue<DateTimeOffset>(4),
                        CreatedBy = ToInt64(pr.GetValue(5)),
                        CreatedAt = pr.IsDBNull(6) ? null : pr.GetFieldValue<DateTimeOffset>(6),
                        Items = new List<PartRequestItemDto>()
                    });
                }
            }

            foreach (var prh in partRequests)
            {
                await using var itCmd = conn.CreateCommand();
                itCmd.BindByName = true;
                itCmd.CommandText = @"
                    SELECT i.item_id,
                           i.request_id,
                           i.part_id,
                           p.part_code,
                           i.qty_requested,
                           i.qty_issued,
                           i.note,
                           i.requested_by,
                           i.requested_at,
                           i.created_by,
                           i.created_at
                    FROM LSTS_PART_REQUEST_ITEMS i
                    JOIN LSTS_PARTS p ON p.part_id = i.part_id
                    WHERE i.request_id = :p_request_id
                    ORDER BY i.item_id";
                itCmd.Parameters.Add(new OracleParameter("p_request_id", OracleDbType.Int64, prh.RequestId, ParameterDirection.Input));

                await using var ir = await itCmd.ExecuteReaderAsync();
                while (await ir.ReadAsync())
                {
                    prh.Items.Add(new PartRequestItemDto
                    {
                        ItemId = ToInt64(ir.GetValue(0)),
                        RequestId = ToInt64(ir.GetValue(1)),
                        PartId = ToInt64(ir.GetValue(2)),
                        PartCode = ir.IsDBNull(3) ? "" : ir.GetString(3),
                        QtyRequested = ToInt32(ir.GetValue(4)),
                        QtyIssued = ToInt32(ir.GetValue(5)),
                        Note = ir.IsDBNull(6) ? null : ir.GetString(6),
                        RequestedBy = ToInt64(ir.GetValue(7)),
                        RequestedAt = ir.IsDBNull(8) ? null : ir.GetFieldValue<DateTimeOffset>(8),
                        CreatedBy = ToInt64(ir.GetValue(9)),
                        CreatedAt = ir.IsDBNull(10) ? null : ir.GetFieldValue<DateTimeOffset>(10),
                    });
                }
            }

            var usageSummary = new List<UsageSummaryDto>();
            await using (var usCmd = conn.CreateCommand())
            {
                usCmd.BindByName = true;
                usCmd.CommandText = @"
                    SELECT u.part_id,
                           p.part_code,
                           SUM(u.qty)                 AS total_qty,
                           SUM(u.qty * u.unit_price)  AS total_amount
                    FROM LSTS_TICKET_PART_USAGE u
                    JOIN LSTS_PARTS p ON p.part_id = u.part_id
                    WHERE u.ticket_id = :p_ticket_id
                    GROUP BY u.part_id, p.part_code
                    ORDER BY p.part_code";
                usCmd.Parameters.Add(new OracleParameter("p_ticket_id", OracleDbType.Int64, ticketId, ParameterDirection.Input));

                await using var ur = await usCmd.ExecuteReaderAsync();
                while (await ur.ReadAsync())
                {
                    usageSummary.Add(new UsageSummaryDto
                    {
                        PartId = ToInt64(ur.GetValue(0)),
                        PartCode = ur.IsDBNull(1) ? "" : ur.GetString(1),
                        TotalQty = ToInt32(ur.GetValue(2)),
                        TotalAmount = ur.IsDBNull(3) ? 0m : Convert.ToDecimal(ur.GetValue(3))
                    });
                }
            }

            var dto = new TicketDashboardDto
            {
                Ticket = ticket,
                PartRequests = partRequests,
                UsageSummary = usageSummary
            };

            return Results.Ok(dto);
        })
        .RequireAuthorization("Tickets.Dashboard");

        return app;
    }
}