using Lsts.Api.Contracts;
using Lsts.Api.Infrastructure;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Lsts.Api.Endpoints;

public static class PublicTicketEndpoints
{
    public static WebApplication MapPublicTicketEndpoints(this WebApplication app)
    {
        
        app.MapGet("/api/public/tickets/by-service", async (
            string serviceNo,
            string last4,
            DbFactory db) =>
        {
            if (string.IsNullOrWhiteSpace(serviceNo))
                return Results.BadRequest(new { error = "serviceNo is required" });

            if (string.IsNullOrWhiteSpace(last4) || last4.Length != 4)
                return Results.BadRequest(new { error = "last4 must be 4 digits" });

            await using var conn = await db.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;

            cmd.CommandText = @"
                SELECT service_no,
                    current_status,
                    status_updated_at,
                    estimated_delivery_date,
                    public_note
                FROM LSTS_PUBLIC_TICKET_LOOKUP
                WHERE service_no = :p_service_no
                AND phone_last4 = :p_last4";

            cmd.Parameters.Add(new OracleParameter("p_service_no", OracleDbType.Varchar2, serviceNo, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("p_last4", OracleDbType.Varchar2, last4, ParameterDirection.Input));

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return Results.NotFound(new { error = "Ticket not found" });

            var dto = MapPublicTicket(reader);
            return Results.Ok(dto);
        })
        .AllowAnonymous();

        app.MapGet("/api/public/tickets/by-serial", async (
            string serialNo,
            string last4,
            DbFactory db) =>
        {
            if (string.IsNullOrWhiteSpace(serialNo))
                return Results.BadRequest(new { error = "serialNo is required" });

            if (string.IsNullOrWhiteSpace(last4) || last4.Length != 4)
                return Results.BadRequest(new { error = "last4 must be 4 digits" });

            await using var conn = await db.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;

            cmd.CommandText = @"
                SELECT service_no,
                    current_status,
                    status_updated_at,
                    estimated_delivery_date,
                    public_note
                FROM LSTS_PUBLIC_TICKET_LOOKUP
                WHERE serial_no = :p_serial_no
                AND phone_last4 = :p_last4";

            cmd.Parameters.Add(new OracleParameter("p_serial_no", OracleDbType.Varchar2, serialNo, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("p_last4", OracleDbType.Varchar2, last4, ParameterDirection.Input));

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return Results.NotFound(new { error = "Ticket not found" });

            var dto = MapPublicTicket(reader);
            return Results.Ok(dto);
        })
        .AllowAnonymous();
        return app;
    }

    private static PublicTicketStatusDto MapPublicTicket(OracleDataReader reader)
    {
        var statusUpdatedAt = reader.IsDBNull(2)
            ? DateTimeOffset.MinValue
            : new DateTimeOffset(reader.GetDateTime(2));

        var estimatedDelivery = reader.IsDBNull(3)
            ? (DateTime?)null
            : reader.GetDateTime(3);

        return new PublicTicketStatusDto
        {
            ServiceNo = reader.IsDBNull(0) ? "" : reader.GetString(0),
            CurrentStatus = reader.IsDBNull(1) ? "" : reader.GetString(1),
            StatusUpdatedAt = statusUpdatedAt,
            EstimatedDeliveryDate = estimatedDelivery,
            PublicNote = reader.IsDBNull(4) ? null : reader.GetString(4),
        };
    }
}