using Lsts.Api.Contracts;
using Lsts.Api.Infrastructure;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using static Lsts.Api.Infrastructure.DbValue;

namespace Lsts.Api.Endpoints;

public static class DevicesEndpoints
{
    public static WebApplication MapDevicesEndpoints(this WebApplication app)
    {
        app.MapPost("/api/devices", async (DeviceCreateRequest req, DbFactory db) =>
        {
            if (req.CustomerId <= 0) return Results.BadRequest(new { error = "customerId must be > 0" });
            if (req.ModelId <= 0) return Results.BadRequest(new { error = "modelId must be > 0" });
            if (string.IsNullOrWhiteSpace(req.DeviceType)) return Results.BadRequest(new { error = "deviceType is required" });
            if (string.IsNullOrWhiteSpace(req.SerialNo)) return Results.BadRequest(new { error = "serialNo is required" });
            if (req.CreatedBy <= 0) return Results.BadRequest(new { error = "createdBy must be > 0" });

            var serial = req.SerialNo.Trim().ToUpperInvariant();
            var deviceType = req.DeviceType.Trim().ToUpperInvariant();

            await using var conn = await db.OpenConnectionAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                await using (var custCmd = conn.CreateCommand())
                {
                    custCmd.Transaction = tx;
                    custCmd.BindByName = true;
                    custCmd.CommandText = @"
                        SELECT COUNT(*)
                        FROM LSTS_CUSTOMERS
                        WHERE customer_id = :p_customer_id
                          AND is_active = 'Y'";
                    custCmd.Parameters.Add(new OracleParameter("p_customer_id", OracleDbType.Int64, req.CustomerId, ParameterDirection.Input));

                    var cnt = Convert.ToInt32(await custCmd.ExecuteScalarAsync());
                    if (cnt == 0)
                    {
                        try { tx.Rollback(); } catch { }
                        return Results.BadRequest(new { error = "customer not found or inactive" });
                    }
                }

                await using (var modelCmd = conn.CreateCommand())
                {
                    modelCmd.Transaction = tx;
                    modelCmd.BindByName = true;
                    modelCmd.CommandText = @"
                        SELECT COUNT(*)
                        FROM LSTS_DEVICE_MODELS
                        WHERE model_id = :p_model_id";
                    modelCmd.Parameters.Add(new OracleParameter("p_model_id", OracleDbType.Int64, req.ModelId, ParameterDirection.Input));

                    var cnt = Convert.ToInt32(await modelCmd.ExecuteScalarAsync());
                    if (cnt == 0)
                    {
                        try { tx.Rollback(); } catch { }
                        return Results.BadRequest(new { error = "model not found" });
                    }
                }

                await using (var dupCmd = conn.CreateCommand())
                {
                    dupCmd.Transaction = tx;
                    dupCmd.BindByName = true;
                    dupCmd.CommandText = @"
                        SELECT device_id
                        FROM LSTS_DEVICES
                        WHERE UPPER(serial_no) = :p_serial
                          AND is_active = 'Y'
                        FETCH FIRST 1 ROWS ONLY";
                    dupCmd.Parameters.Add(new OracleParameter("p_serial", OracleDbType.Varchar2, serial, ParameterDirection.Input));

                    var existing = await dupCmd.ExecuteScalarAsync();
                    if (existing is not null && existing is not DBNull)
                    {
                        try { tx.Rollback(); } catch { }
                        return Results.Conflict(new { error = "serial already exists", deviceId = ToInt64(existing) });
                    }
                }

                long newDeviceId;
                await using (var insCmd = conn.CreateCommand())
                {
                    insCmd.Transaction = tx;
                    insCmd.BindByName = true;

                    insCmd.CommandText = @"
                        INSERT INTO LSTS_DEVICES
                        (customer_id, device_type, model_id, serial_no, notes, is_active, created_at, created_by)
                        VALUES
                        (:p_customer_id, :p_device_type, :p_model_id, :p_serial_no, :p_notes, 'Y', SYSTIMESTAMP, :p_created_by)
                        RETURNING device_id INTO :o_device_id";

                    insCmd.Parameters.Add(new OracleParameter("p_customer_id", OracleDbType.Int64, req.CustomerId, ParameterDirection.Input));
                    insCmd.Parameters.Add(new OracleParameter("p_device_type", OracleDbType.Varchar2, deviceType, ParameterDirection.Input));
                    insCmd.Parameters.Add(new OracleParameter("p_model_id", OracleDbType.Int64, req.ModelId, ParameterDirection.Input));
                    insCmd.Parameters.Add(new OracleParameter("p_serial_no", OracleDbType.Varchar2, serial, ParameterDirection.Input));
                    insCmd.Parameters.Add(new OracleParameter("p_notes", OracleDbType.Varchar2, (object?)req.Notes ?? DBNull.Value, ParameterDirection.Input));
                    insCmd.Parameters.Add(new OracleParameter("p_created_by", OracleDbType.Int64, req.CreatedBy, ParameterDirection.Input));

                    var oId = new OracleParameter("o_device_id", OracleDbType.Int64) { Direction = ParameterDirection.Output };
                    insCmd.Parameters.Add(oId);

                    await insCmd.ExecuteNonQueryAsync();
                    newDeviceId = ToInt64(oId.Value);
                }

                tx.Commit();

                await using (var selCmd = conn.CreateCommand())
                {
                    selCmd.BindByName = true;
                    selCmd.CommandText = @"
                        SELECT
                            device_id,
                            serial_no,
                            customer_id,
                            device_type,
                            model_id,
                            model_code,
                            model_name,
                            model_no,
                            cpu,
                            gpu,
                            ram_summary,
                            screen_summary,
                            brand_id,
                            brand_code
                        FROM LSTS_V_DEVICE_PICKLIST
                        WHERE device_id = :p_device_id
                        FETCH FIRST 1 ROWS ONLY";

                    selCmd.Parameters.Add(new OracleParameter("p_device_id", OracleDbType.Int64, newDeviceId, ParameterDirection.Input));

                    await using var r = await selCmd.ExecuteReaderAsync();
                    if (!await r.ReadAsync())
                    {
                        return Results.Created($"/api/devices/{newDeviceId}", new
                        {
                            deviceId = newDeviceId,
                            serialNo = serial,
                            customerId = req.CustomerId,
                            modelId = req.ModelId,
                            deviceType
                        });
                    }

                    var dto = new DevicePicklistItemDto(
                        DeviceId: ToInt64(r.GetValue(0)),
                        SerialNo: r.IsDBNull(1) ? "" : r.GetString(1),
                        CustomerId: ToInt64(r.GetValue(2)),
                        DeviceType: r.IsDBNull(3) ? "" : r.GetString(3),
                        ModelId: ToInt64(r.GetValue(4)),
                        ModelCode: r.IsDBNull(5) ? "" : r.GetString(5),
                        ModelName: r.IsDBNull(6) ? "" : r.GetString(6),
                        ModelNo: r.IsDBNull(7) ? "" : r.GetString(7),
                        Cpu: r.IsDBNull(8) ? null : r.GetString(8),
                        Gpu: r.IsDBNull(9) ? null : r.GetString(9),
                        RamSummary: r.IsDBNull(10) ? null : r.GetString(10),
                        ScreenSummary: r.IsDBNull(11) ? null : r.GetString(11),
                        BrandId: ToInt64(r.GetValue(12)),
                        BrandCode: r.IsDBNull(13) ? "" : r.GetString(13)
                    );

                    return Results.Created($"/api/devices/{newDeviceId}", dto);
                }
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        })
        .RequireAuthorization("Devices.Write");

        app.MapGet("/api/devices/{deviceId:long}", async (long deviceId, DbFactory db) =>
        {
            await using var conn = await db.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;

            cmd.CommandText = @"
                SELECT
                    device_id,
                    serial_no,
                    customer_id,
                    device_type,
                    model_id,
                    model_code,
                    model_name,
                    model_no,
                    cpu,
                    gpu,
                    ram_summary,
                    screen_summary,
                    brand_id,
                    brand_code
                FROM LSTS_V_DEVICE_PICKLIST
                WHERE device_id = :p_device_id
                FETCH FIRST 1 ROWS ONLY";

            cmd.Parameters.Add(new OracleParameter(
                "p_device_id",
                OracleDbType.Int64,
                deviceId,
                ParameterDirection.Input));

            await using var r = await cmd.ExecuteReaderAsync();

            if (!await r.ReadAsync())
                return Results.NotFound(new { error = "Device not found", deviceId });

            var dto = new DevicePicklistItemDto(
                DeviceId: ToInt64(r.GetValue(0)),
                SerialNo: r.IsDBNull(1) ? "" : r.GetString(1),
                CustomerId: ToInt64(r.GetValue(2)),
                DeviceType: r.IsDBNull(3) ? "" : r.GetString(3),
                ModelId: ToInt64(r.GetValue(4)),
                ModelCode: r.IsDBNull(5) ? "" : r.GetString(5),
                ModelName: r.IsDBNull(6) ? "" : r.GetString(6),
                ModelNo: r.IsDBNull(7) ? "" : r.GetString(7),
                Cpu: r.IsDBNull(8) ? null : r.GetString(8),
                Gpu: r.IsDBNull(9) ? null : r.GetString(9),
                RamSummary: r.IsDBNull(10) ? null : r.GetString(10),
                ScreenSummary: r.IsDBNull(11) ? null : r.GetString(11),
                BrandId: ToInt64(r.GetValue(12)),
                BrandCode: r.IsDBNull(13) ? "" : r.GetString(13)
            );

            return Results.Ok(dto);
        })
        .RequireAuthorization("Devices.Read");

        app.MapGet("/api/devices/{deviceId:long}/summary", async (long deviceId, DbFactory db) =>
        {
            await using var conn = await db.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;

            cmd.CommandText = @"
                select
                    p.device_id,
                    p.serial_no,
                    p.customer_id,
                    p.device_type,
                    p.model_id,
                    p.model_code,
                    p.model_name,
                    p.model_no,
                    ms.cpu,
                    ms.gpu,
                    coalesce(p.ram_summary, ms.ram)         as ram_summary,
                    coalesce(p.screen_summary, ms.display)  as screen_summary,
                    p.brand_id,
                    p.brand_code
                from lsts_v_device_picklist p
                left join lsts_v_model_specs ms on ms.model_id = p.model_id
                where p.device_id = :deviceId";

            cmd.Parameters.Add(new OracleParameter("deviceId", OracleDbType.Int64) { Value = deviceId });

            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
                return Results.NotFound(new { error = "Device not found" });

            var dto = new DevicePicklistItemDto(
                DeviceId: ToInt64(r.GetValue(0)),
                SerialNo: r.IsDBNull(1) ? "" : r.GetString(1),
                CustomerId: ToInt64(r.GetValue(2)),
                DeviceType: r.IsDBNull(3) ? "" : r.GetString(3),
                ModelId: ToInt64(r.GetValue(4)),
                ModelCode: r.IsDBNull(5) ? "" : r.GetString(5),
                ModelName: r.IsDBNull(6) ? "" : r.GetString(6),
                ModelNo: r.IsDBNull(7) ? "" : r.GetString(7),
                Cpu: r.IsDBNull(8) ? null : r.GetString(8),
                Gpu: r.IsDBNull(9) ? null : r.GetString(9),
                RamSummary: r.IsDBNull(10) ? null : r.GetString(10),
                ScreenSummary: r.IsDBNull(11) ? null : r.GetString(11),
                BrandId: ToInt64(r.GetValue(12)),
                BrandCode: r.IsDBNull(13) ? "" : r.GetString(13)
            );

            return Results.Ok(dto);
        })
        .RequireAuthorization("Devices.Read");

        app.MapGet("/api/devices/picklist", async (
            long? customerId,
            string? q,
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
                    device_id,
                    serial_no,
                    customer_id,
                    device_type,
                    model_id,
                    model_code,
                    model_name,
                    model_no,
                    cpu,
                    gpu,
                    ram_summary,
                    screen_summary,
                    brand_id,
                    brand_code
                FROM LSTS_V_DEVICE_PICKLIST
                WHERE (:p_customer_id IS NULL OR customer_id = :p_customer_id)
                  AND (
                        :p_q IS NULL
                        OR UPPER(serial_no)   LIKE '%' || UPPER(:p_q) || '%'
                        OR UPPER(model_code)  LIKE '%' || UPPER(:p_q) || '%'
                        OR UPPER(model_name)  LIKE '%' || UPPER(:p_q) || '%'
                        OR UPPER(model_no)    LIKE '%' || UPPER(:p_q) || '%'
                        OR UPPER(brand_code)  LIKE '%' || UPPER(:p_q) || '%'
                      )
                ORDER BY device_id DESC
                FETCH FIRST {take} ROWS ONLY";

            cmd.Parameters.Add(new OracleParameter(
                "p_customer_id",
                OracleDbType.Int64,
                (object?)customerId ?? DBNull.Value,
                ParameterDirection.Input));

            cmd.Parameters.Add(new OracleParameter(
                "p_q",
                OracleDbType.Varchar2,
                (object?)search ?? DBNull.Value,
                ParameterDirection.Input));

            var list = new List<DevicePicklistItemDto>();
            await using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                list.Add(new DevicePicklistItemDto(
                    DeviceId: ToInt64(r.GetValue(0)),
                    SerialNo: r.IsDBNull(1) ? "" : r.GetString(1),
                    CustomerId: ToInt64(r.GetValue(2)),
                    DeviceType: r.IsDBNull(3) ? "" : r.GetString(3),
                    ModelId: ToInt64(r.GetValue(4)),
                    ModelCode: r.IsDBNull(5) ? "" : r.GetString(5),
                    ModelName: r.IsDBNull(6) ? "" : r.GetString(6),
                    ModelNo: r.IsDBNull(7) ? "" : r.GetString(7),
                    Cpu: r.IsDBNull(8) ? null : r.GetString(8),
                    Gpu: r.IsDBNull(9) ? null : r.GetString(9),
                    RamSummary: r.IsDBNull(10) ? null : r.GetString(10),
                    ScreenSummary: r.IsDBNull(11) ? null : r.GetString(11),
                    BrandId: ToInt64(r.GetValue(12)),
                    BrandCode: r.IsDBNull(13) ? "" : r.GetString(13)
                ));
            }

            return Results.Ok(list);
        })
        .RequireAuthorization("Devices.Read");

        return app;
    }

    public record DeviceCreateRequest(
        long CustomerId,
        string DeviceType,
        long ModelId,
        string SerialNo,
        string? Notes,
        long CreatedBy
    );
}