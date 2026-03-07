using Lsts.Api.Infrastructure;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using static Lsts.Api.Infrastructure.DbValue;

namespace Lsts.Api.Endpoints;

public static class ModelsEndpoints
{
    public static WebApplication MapModelsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/models/picklist", async (
            string? q,
            long? brandId,
            int? limit,
            DbFactory db) =>
        {
            var take = Math.Clamp(limit ?? 50, 1, 200);
            var search = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

            await using var conn = await db.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;

            cmd.CommandText = $@"
                SELECT
                    m.model_id,
                    m.brand_id,
                    b.brand_code,
                    b.brand_name,
                    m.model_code,
                    m.model_name,
                    m.model_no,
                    m.cpu,
                    m.gpu,
                    m.ram_summary,
                    m.screen_summary
                FROM LSTS_DEVICE_MODELS m
                JOIN LSTS_DEVICE_BRANDS b ON b.brand_id = m.brand_id
                WHERE m.is_active = 'Y'
                AND b.is_active = 'Y'
                AND (:p_brand_id IS NULL OR m.brand_id = :p_brand_id)
                AND (
                        :p_q IS NULL
                        OR UPPER(m.model_code) LIKE '%' || UPPER(:p_q) || '%'
                        OR UPPER(m.model_name) LIKE '%' || UPPER(:p_q) || '%'
                        OR UPPER(m.model_no)   LIKE '%' || UPPER(:p_q) || '%'
                        OR UPPER(NVL(b.brand_code,'')) LIKE '%' || UPPER(:p_q) || '%'
                        OR UPPER(NVL(b.brand_name,'')) LIKE '%' || UPPER(:p_q) || '%'
                        OR TO_CHAR(m.model_id) = :p_q
                    )
                ORDER BY b.brand_code, m.model_code
                FETCH FIRST {take} ROWS ONLY";

            cmd.Parameters.Add(new OracleParameter("p_brand_id", OracleDbType.Int64,
                (object?)brandId ?? DBNull.Value, ParameterDirection.Input));

            cmd.Parameters.Add(new OracleParameter("p_q", OracleDbType.Varchar2,
                (object?)search ?? DBNull.Value, ParameterDirection.Input));

            var list = new List<object>();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new
                {
                    modelId = ToInt64(r.GetValue(0)),
                    brandId = ToInt64(r.GetValue(1)),
                    brandCode = r.IsDBNull(2) ? "" : r.GetString(2),
                    brandName = r.IsDBNull(3) ? "" : r.GetString(3),
                    modelCode = r.IsDBNull(4) ? "" : r.GetString(4),
                    modelName = r.IsDBNull(5) ? "" : r.GetString(5),
                    modelNo = r.IsDBNull(6) ? "" : r.GetString(6),
                    cpu = r.IsDBNull(7) ? null : r.GetString(7),
                    gpu = r.IsDBNull(8) ? null : r.GetString(8),
                    ramSummary = r.IsDBNull(9) ? null : r.GetString(9),
                    screenSummary = r.IsDBNull(10) ? null : r.GetString(10),
                });
            }

            return Results.Ok(list);
        })
        .RequireAuthorization("Devices.Read");

        return app;
    }
}