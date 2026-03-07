using Lsts.Api.Contracts;
using Lsts.Api.Infrastructure;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using static Lsts.Api.Infrastructure.DbValue;
using Oracle.ManagedDataAccess.Types;

namespace Lsts.Api.Endpoints;

public static class TicketStatusesEndpoints
{
    public static WebApplication MapTicketsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/tickets", async (
    int? limit,
    string? q,
    string? status,
    bool? openOnly,
    DbFactory db) =>
{
    var take = Math.Clamp(limit ?? 200, 1, 500);

    var qq = string.IsNullOrWhiteSpace(q) ? null : q.Trim().ToUpperInvariant();
    var st = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();
    var open = openOnly ?? true;

    await using var conn = await db.OpenConnectionAsync();
    await using var cmd = conn.CreateCommand();
    cmd.BindByName = true;

    cmd.CommandText = $@"
        SELECT
            ticket_id,
            service_no,
            current_status,
            fault_desc,
            NVL(status_updated_at, created_at) AS at
        FROM LSTS_TICKETS
        WHERE (:p_open_only = 0 OR NVL(UPPER(current_status),'') NOT IN ('CLOSED','CANCELED','CANCELLED'))
          AND (:p_status IS NULL OR UPPER(current_status) = :p_status)
          AND (:p_q IS NULL
               OR UPPER(service_no) LIKE '%' || :p_q || '%'
               OR TO_CHAR(ticket_id) LIKE '%' || :p_q || '%'
               OR NVL(UPPER(fault_desc),'') LIKE '%' || :p_q || '%')
        ORDER BY NVL(status_updated_at, created_at) DESC, ticket_id DESC
        FETCH FIRST {take} ROWS ONLY";

    cmd.Parameters.Add(new OracleParameter("p_open_only", OracleDbType.Int32, open ? 1 : 0, ParameterDirection.Input));
    cmd.Parameters.Add(new OracleParameter("p_status", OracleDbType.Varchar2, (object?)st ?? DBNull.Value, ParameterDirection.Input));
    cmd.Parameters.Add(new OracleParameter("p_q", OracleDbType.Varchar2, (object?)qq ?? DBNull.Value, ParameterDirection.Input));

    var rows = new List<object>();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        rows.Add(new
        {
            ticketId = ToInt64(r.GetValue(0)),
            serviceNo = r.IsDBNull(1) ? "" : r.GetString(1),
            currentStatus = r.IsDBNull(2) ? "" : r.GetString(2),
            faultDesc = r.IsDBNull(3) ? "" : r.GetString(3),
            at = r.IsDBNull(4) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(4)
        });
    }

    return Results.Ok(rows);
})
.RequireAuthorization("Tickets.Dashboard");

app.MapPost("/api/tickets", async (
            TicketCreateRequest req,
            DbFactory db) =>
        {
            if (req.DeviceId <= 0)
                return Results.BadRequest(new { error = "deviceId must be > 0" });

            if (string.IsNullOrWhiteSpace(req.FaultDesc))
                return Results.BadRequest(new { error = "faultDesc is required" });

            if (req.CreatedBy <= 0)
                return Results.BadRequest(new { error = "createdBy must be > 0" });

            await using var conn = await db.OpenConnectionAsync();
            using var tx = conn.BeginTransaction();

            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.BindByName = true;

            cmd.CommandText = @"
                INSERT INTO LSTS_TICKETS
                (device_id, fault_desc, internal_notes, public_note, estimated_delivery_date, created_by)
                VALUES
                (:p_device_id, :p_fault_desc, :p_internal_notes, :p_public_note, :p_est_delivery, :p_created_by)
                RETURNING ticket_id, service_no, current_status
                INTO :o_ticket_id, :o_service_no, :o_current_status";

            cmd.Parameters.Add(new OracleParameter("p_device_id", OracleDbType.Int64, req.DeviceId, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("p_fault_desc", OracleDbType.Varchar2, req.FaultDesc, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("p_internal_notes", OracleDbType.Varchar2, (object?)req.InternalNotes ?? DBNull.Value, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("p_public_note", OracleDbType.Varchar2, (object?)req.PublicNote ?? DBNull.Value, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("p_est_delivery", OracleDbType.Date, (object?)req.EstimatedDeliveryDate ?? DBNull.Value, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("p_created_by", OracleDbType.Int64, req.CreatedBy, ParameterDirection.Input));

            var oTicketId = new OracleParameter("o_ticket_id", OracleDbType.Int64) { Direction = ParameterDirection.Output };
            var oServiceNo = new OracleParameter("o_service_no", OracleDbType.Varchar2, 50) { Direction = ParameterDirection.Output };
            var oCurrentStatus = new OracleParameter("o_current_status", OracleDbType.Varchar2, 20) { Direction = ParameterDirection.Output };

            cmd.Parameters.Add(oTicketId);
            cmd.Parameters.Add(oServiceNo);
            cmd.Parameters.Add(oCurrentStatus);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }

            var ticketId = ToInt64(oTicketId.Value);
            var serviceNo = oServiceNo.Value is OracleString osn ? osn.Value : Convert.ToString(oServiceNo.Value);
            var currentStatus = oCurrentStatus.Value is OracleString ocs ? ocs.Value : Convert.ToString(oCurrentStatus.Value);

            return Results.Created($"/api/tickets/{ticketId}", new TicketCreateResponse
            {
                TicketId = ticketId,
                ServiceNo = serviceNo ?? "",
                CurrentStatus = currentStatus ?? ""
            });
        })
        .RequireAuthorization("Tickets.Create");

        app.MapPatch("/api/tickets/{ticketId:long}/status", async (
            long ticketId,
            TicketStatusUpdateRequest req,
            DbFactory db) =>
        {
            if (ticketId <= 0)
                return Results.BadRequest(new { error = "ticketId must be > 0" });

            if (string.IsNullOrWhiteSpace(req.ToStatus))
                return Results.BadRequest(new { error = "toStatus is required" });

            if (req.ChangedBy <= 0)
                return Results.BadRequest(new { error = "changedBy must be > 0" });

            await using var conn = await db.OpenConnectionAsync();
            using var tx = conn.BeginTransaction();

            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.BindByName = true;

            cmd.CommandText = @"
                UPDATE LSTS_TICKETS
                SET current_status = :p_to_status,
                    updated_by     = :p_changed_by,
                    updated_at     = SYSTIMESTAMP
                WHERE ticket_id      = :p_ticket_id
                RETURNING service_no, current_status
                INTO :o_service_no, :o_current_status";

            cmd.Parameters.Add(new OracleParameter("p_to_status", OracleDbType.Varchar2, req.ToStatus, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("p_changed_by", OracleDbType.Int64, req.ChangedBy, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("p_ticket_id", OracleDbType.Int64, ticketId, ParameterDirection.Input));

            var oServiceNo = new OracleParameter("o_service_no", OracleDbType.Varchar2, 50) { Direction = ParameterDirection.Output };
            var oCurrentStatus = new OracleParameter("o_current_status", OracleDbType.Varchar2, 20) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(oServiceNo);
            cmd.Parameters.Add(oCurrentStatus);

            try
            {
                var affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0)
                {
                    try { tx.Rollback(); } catch { }
                    return Results.NotFound(new { error = "Ticket not found" });
                }

                tx.Commit();
            }
            catch (OracleException ex) when (ex.Number == 2290)
            {
                try { tx.Rollback(); } catch { }
                return Results.BadRequest(new { error = "Invalid status value (DB constraint failed)" });
            }
            catch (OracleException ex) when (ex.Number == 2291)
            {
                try { tx.Rollback(); } catch { }
                return Results.BadRequest(new { error = "Invalid foreign key (ticket/user not found)" });
            }

            var serviceNo = oServiceNo.Value is OracleString osn ? osn.Value : Convert.ToString(oServiceNo.Value);
            var currentStatus = oCurrentStatus.Value is OracleString ocs ? ocs.Value : Convert.ToString(oCurrentStatus.Value);

            return Results.Ok(new TicketStatusUpdateResponse
            {
                TicketId = ticketId,
                ServiceNo = serviceNo ?? "",
                CurrentStatus = currentStatus ?? ""
            });
        })
        .RequireAuthorization("Tickets.Update");

        app.MapPatch("/api/tickets/{ticketId:long}/notes", async (
            long ticketId,
            TicketNotesUpdateRequest req,
            DbFactory db) =>
        {
            if (ticketId <= 0)
                return Results.BadRequest(new { error = "ticketId must be > 0" });

            if (req.UpdatedBy <= 0)
                return Results.BadRequest(new { error = "updatedBy must be > 0" });

            await using var conn = await db.OpenConnectionAsync();
            await using var tx = conn.BeginTransaction();

            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.BindByName = true;

            cmd.CommandText = @"
                UPDATE LSTS_TICKETS
                SET internal_notes = :p_internal_notes,
                    public_note    = :p_public_note,
                    updated_by     = :p_updated_by,
                    updated_at     = SYSTIMESTAMP
                WHERE ticket_id      = :p_ticket_id";

            cmd.Parameters.Add(new OracleParameter("p_internal_notes", OracleDbType.Varchar2,
                (object?)req.InternalNotes ?? DBNull.Value, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("p_public_note", OracleDbType.Varchar2,
                (object?)req.PublicNote ?? DBNull.Value, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("p_updated_by", OracleDbType.Int64,
                req.UpdatedBy, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("p_ticket_id", OracleDbType.Int64,
                ticketId, ParameterDirection.Input));

            try
            {
                var affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0)
                {
                    try { tx.Rollback(); } catch { }
                    return Results.NotFound(new { error = "Ticket not found" });
                }

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }

            return Results.Ok(new TicketNotesUpdateResponse(
                TicketId: ticketId,
                InternalNotes: req.InternalNotes,
                PublicNote: req.PublicNote
            ));
        })
        .RequireAuthorization("Tickets.Update");

        return app;
    }
}