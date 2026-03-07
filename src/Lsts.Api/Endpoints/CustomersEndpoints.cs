using Lsts.Api.Infrastructure;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using static Lsts.Api.Infrastructure.DbValue;

namespace Lsts.Api.Endpoints;

public static class CustomersEndpoints
{
    public static WebApplication MapCustomersEndpoints(this WebApplication app)
    {
        app.MapGet("/api/customers/picklist", async (string? q, int? limit, DbFactory db) =>
        {
            var take = Math.Clamp(limit ?? 30, 1, 200);
            var search = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

            await using var conn = await db.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.BindByName = true;

            cmd.CommandText = $@"
                SELECT customer_id,
                    full_name,
                    phone_digits,
                    phone_last4,
                    email
                FROM   LSTS_CUSTOMERS
                WHERE  is_active = 'Y'
                AND   (
                        :p_q IS NULL
                        OR UPPER(full_name) LIKE '%' || UPPER(:p_q) || '%'
                        OR phone_digits     LIKE '%' || :p_q || '%'
                        OR phone_last4      LIKE '%' || :p_q || '%'
                        OR UPPER(NVL(email,'')) LIKE '%' || UPPER(:p_q) || '%'
                        OR TO_CHAR(customer_id) = :p_q
                    )
                ORDER BY customer_id DESC
                FETCH FIRST {take} ROWS ONLY";

            cmd.Parameters.Add(new OracleParameter("p_q", OracleDbType.Varchar2,
                (object?)search ?? DBNull.Value, ParameterDirection.Input));

            var list = new List<object>();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new
                {
                    customerId = ToInt64(r.GetValue(0)),
                    fullName = r.IsDBNull(1) ? "" : r.GetString(1),
                    phoneDigits = r.IsDBNull(2) ? "" : r.GetString(2),
                    phoneLast4 = r.IsDBNull(3) ? null : r.GetString(3),
                    email = r.IsDBNull(4) ? null : r.GetString(4),
                });
            }

            return Results.Ok(list);
        })
        .RequireAuthorization("Customers.Read");

        app.MapPost("/api/customers", async (CustomerCreateRequest req, DbFactory db, ILoggerFactory loggerFactory) =>
        {
            var log = loggerFactory.CreateLogger("Customers.Create");

            if (string.IsNullOrWhiteSpace(req.FullName))
                return Results.BadRequest(new { error = "fullName is required" });

            if (string.IsNullOrWhiteSpace(req.PhoneDigits))
                return Results.BadRequest(new { error = "phoneDigits is required" });

            if (req.CreatedBy <= 0)
                return Results.BadRequest(new { error = "createdBy must be > 0" });

            var digits = new string(req.PhoneDigits.Where(char.IsDigit).ToArray());

            if (digits.Length < 10 || digits.Length > 15)
                return Results.BadRequest(new { error = "phoneDigits must be 10-15 digits" });

            var last4 = digits.Length >= 4 ? digits[^4..] : digits;

            await using var conn = await db.OpenConnectionAsync();
            await using var tx = conn.BeginTransaction();

            try
            {
                long? existingId = null;
                string? existingActive = null;

                await using (var dupCmd = conn.CreateCommand())
                {
                    dupCmd.Transaction = tx;
                    dupCmd.BindByName = true;
                    dupCmd.CommandText = @"
                        SELECT customer_id, is_active
                        FROM LSTS_CUSTOMERS
                        WHERE phone_digits = :p_digits
                        FETCH FIRST 1 ROWS ONLY";
                    dupCmd.Parameters.Add(new OracleParameter("p_digits", OracleDbType.Varchar2, digits, ParameterDirection.Input));

                    await using var rr = await dupCmd.ExecuteReaderAsync();
                    if (await rr.ReadAsync())
                    {
                        existingId = ToInt64(rr.GetValue(0));
                        existingActive = rr.IsDBNull(1) ? null : rr.GetString(1);
                    }
                }

                if (existingId.HasValue)
                {
                    if (string.Equals(existingActive, "Y", StringComparison.OrdinalIgnoreCase))
                    {
                        try { tx.Rollback(); } catch { }
                        return Results.Conflict(new { error = "phoneDigits already exists", customerId = existingId.Value });
                    }
                    await using (var upd = conn.CreateCommand())
                    {
                        upd.Transaction = tx;
                        upd.BindByName = true;
                        upd.CommandText = @"
                            UPDATE LSTS_CUSTOMERS
                            SET full_name    = :p_full_name,
                                email        = :p_email,
                                address_text = :p_addr,
                                notes        = :p_notes,
                                is_active    = 'Y',
                                updated_at   = SYSTIMESTAMP,
                                updated_by   = :p_updated_by
                            WHERE customer_id = :p_id";

                        upd.Parameters.Add(new OracleParameter("p_full_name", OracleDbType.Varchar2, req.FullName.Trim(), ParameterDirection.Input));
                        upd.Parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2, (object?)req.Email?.Trim() ?? DBNull.Value, ParameterDirection.Input));
                        upd.Parameters.Add(new OracleParameter("p_addr", OracleDbType.Varchar2, (object?)req.AddressText?.Trim() ?? DBNull.Value, ParameterDirection.Input));
                        upd.Parameters.Add(new OracleParameter("p_notes", OracleDbType.Varchar2, (object?)req.Notes?.Trim() ?? DBNull.Value, ParameterDirection.Input));
                        upd.Parameters.Add(new OracleParameter("p_updated_by", OracleDbType.Int64, req.CreatedBy, ParameterDirection.Input));
                        upd.Parameters.Add(new OracleParameter("p_id", OracleDbType.Int64, existingId.Value, ParameterDirection.Input));

                        await upd.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();

                    return Results.Ok(new
                    {
                        customerId = existingId.Value,
                        reactivated = true
                    });
                }
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.BindByName = true;

                    cmd.CommandText = @"
                        INSERT INTO LSTS_CUSTOMERS
                        (full_name, phone_digits, email, address_text, notes, is_active, created_at, created_by)
                        VALUES
                        (:p_full_name, :p_phone_digits, :p_email, :p_address_text, :p_notes, 'Y', SYSTIMESTAMP, :p_created_by)
                        RETURNING customer_id INTO :o_customer_id";

                    cmd.Parameters.Add(new OracleParameter("p_full_name", OracleDbType.Varchar2, req.FullName.Trim(), ParameterDirection.Input));
                    cmd.Parameters.Add(new OracleParameter("p_phone_digits", OracleDbType.Varchar2, digits, ParameterDirection.Input));
                    cmd.Parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2, (object?)req.Email?.Trim() ?? DBNull.Value, ParameterDirection.Input));
                    cmd.Parameters.Add(new OracleParameter("p_address_text", OracleDbType.Varchar2, (object?)req.AddressText?.Trim() ?? DBNull.Value, ParameterDirection.Input));
                    cmd.Parameters.Add(new OracleParameter("p_notes", OracleDbType.Varchar2, (object?)req.Notes?.Trim() ?? DBNull.Value, ParameterDirection.Input));
                    cmd.Parameters.Add(new OracleParameter("p_created_by", OracleDbType.Int64, req.CreatedBy, ParameterDirection.Input));

                    var oId = new OracleParameter("o_customer_id", OracleDbType.Int64) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(oId);

                    await cmd.ExecuteNonQueryAsync();

                    var customerId = ToInt64(oId.Value);
                    await tx.CommitAsync();

                    return Results.Created($"/api/customers/{customerId}", new
                    {
                        customerId,
                        fullName = req.FullName.Trim(),
                        phoneDigits = digits,
                        phoneLast4 = last4,
                        email = req.Email?.Trim()
                    });
                }
            }
            catch (OracleException ex) when (ex.Number == 2291)
            {
                try { await tx.RollbackAsync(); } catch { }
                log.LogError(ex, "Oracle FK error while creating customer");
                return Results.BadRequest(new { error = "Invalid foreign key (createdBy user not found?)", detail = ex.Message });
            }
            catch (OracleException ex) when (ex.Number == 1400)
            {
                try { await tx.RollbackAsync(); } catch { }
                log.LogError(ex, "Oracle NOT NULL error while creating customer");
                return Results.BadRequest(new { error = "Missing required field", detail = ex.Message });
            }
            catch (OracleException ex) when (ex.Number == 12899)
            {
                try { await tx.RollbackAsync(); } catch { }
                log.LogError(ex, "Oracle length error while creating customer");
                return Results.BadRequest(new { error = "Field too long", detail = ex.Message });
            }
            catch (OracleException ex) when (ex.Number == 1)
            {
                try { await tx.RollbackAsync(); } catch { }
                log.LogError(ex, "Oracle unique constraint error while creating customer");
                return Results.Conflict(new { error = "Duplicate value (unique constraint)", detail = ex.Message });
            }
            catch (OracleException ex)
            {
                try { await tx.RollbackAsync(); } catch { }
                log.LogError(ex, "Oracle error while creating customer");
                return Results.Problem(title: "Oracle error", detail: ex.Message, statusCode: 500);
            }
        })
        .RequireAuthorization("Customers.Write");

        return app;
    }

    public record CustomerCreateRequest(
        string FullName,
        string PhoneDigits,
        string? Email,
        string? AddressText,
        string? Notes,
        long CreatedBy
    );
}