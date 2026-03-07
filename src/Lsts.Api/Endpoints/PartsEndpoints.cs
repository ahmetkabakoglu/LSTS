using Lsts.Api.Contracts;
using Lsts.Api.Infrastructure;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using static Lsts.Api.Infrastructure.DbValue;

namespace Lsts.Api.Endpoints;

public static class PartsEndpoints
{
    public static WebApplication MapPartsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/parts", async (
            string? prefix,
            string? query,
            int? limit,
            DbFactory db) =>
        {
            var p = string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim();
            var q = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
            var take = Math.Clamp(limit ?? 20, 1, 100);

            await using var conn = await db.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;

            cmd.CommandText = $@"
                SELECT part_id,
                       part_code,
                       part_name,
                       on_hand_qty,
                       NVL(sale_price, 0) AS unit_price
                FROM LSTS_PARTS
                WHERE (:p_prefix IS NULL OR UPPER(part_code) LIKE UPPER(:p_prefix) || '%')
                  AND (
                        :p_q IS NULL
                        OR UPPER(part_code) LIKE '%' || UPPER(:p_q) || '%'
                        OR UPPER(NVL(part_name, '')) LIKE '%' || UPPER(:p_q) || '%'
                      )
                ORDER BY part_code
                FETCH FIRST {take} ROWS ONLY";

            cmd.Parameters.Add(new OracleParameter("p_prefix", OracleDbType.Varchar2,
                (object?)p ?? DBNull.Value, ParameterDirection.Input));

            cmd.Parameters.Add(new OracleParameter("p_q", OracleDbType.Varchar2,
                (object?)q ?? DBNull.Value, ParameterDirection.Input));

            var list = new List<PartListItemDto>();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new PartListItemDto(
                    PartId: ToInt64(r.GetValue(0)),
                    PartCode: r.GetString(1),
                    PartName: r.IsDBNull(2) ? "" : r.GetString(2),
                    OnHandQty: ToInt32(r.GetValue(3)),
                    UnitPrice: ToDecimal(r.GetValue(4))
                ));
            }

            return Results.Ok(list);
        })
        .RequireAuthorization("Parts.Read");

        app.MapGet("/api/parts/{partId:long}", async (long partId, DbFactory db) =>
        {
            if (partId <= 0) return Results.BadRequest(new { error = "partId must be > 0" });

            await using var conn = await db.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;

            cmd.CommandText = @"
                SELECT part_id,
                       part_code,
                       part_name,
                       on_hand_qty,
                       NVL(sale_price, 0) AS unit_price
                FROM LSTS_PARTS
                WHERE part_id = :p_part_id";

            cmd.Parameters.Add(new OracleParameter("p_part_id", OracleDbType.Int64, partId, ParameterDirection.Input));

            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
                return Results.NotFound(new { error = "part not found" });

            return Results.Ok(new PartGetDto(
                PartId: ToInt64(r.GetValue(0)),
                PartCode: r.GetString(1),
                PartName: r.IsDBNull(2) ? "" : r.GetString(2),
                OnHandQty: ToInt32(r.GetValue(3)),
                UnitPrice: ToDecimal(r.GetValue(4))
            ));
        })
        .RequireAuthorization("Parts.Read");

        app.MapGet("/api/parts/{partCode}", async (
            string partCode,
            DbFactory db) =>
        {
            if (string.IsNullOrWhiteSpace(partCode))
                return Results.BadRequest(new { error = "partCode is required" });

            await using var conn = await db.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;

            cmd.CommandText = @"
                SELECT part_id,
                       part_code,
                       part_name,
                       on_hand_qty,
                       NVL(sale_price, 0) AS unit_price
                FROM LSTS_PARTS
                WHERE part_code = :p_code";

            cmd.Parameters.Add(new OracleParameter("p_code", OracleDbType.Varchar2,
                partCode.Trim(), ParameterDirection.Input));

            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
                return Results.NotFound(new { error = "part not found" });

            return Results.Ok(new PartGetDto(
                PartId: ToInt64(r.GetValue(0)),
                PartCode: r.GetString(1),
                PartName: r.IsDBNull(2) ? "" : r.GetString(2),
                OnHandQty: ToInt32(r.GetValue(3)),
                UnitPrice: ToDecimal(r.GetValue(4))
            ));
        })
        .RequireAuthorization("Parts.Read");

        return app;
    }
}