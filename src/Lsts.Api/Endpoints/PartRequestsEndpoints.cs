    using Lsts.Api.Contracts;
    using Lsts.Api.Infrastructure;
    using Oracle.ManagedDataAccess.Client;
    using System.Data;
    using static Lsts.Api.Infrastructure.DbValue;
    using Oracle.ManagedDataAccess.Types;

    namespace Lsts.Api.Endpoints;

    public static class PartRequestsEndpoints
    {
        public static WebApplication MapPartRequestsEndpoints(this WebApplication app)
        {
            app.MapGet("/api/part-requests/active", async (int? limit, DbFactory db) =>
            {
                var take = Math.Clamp(limit ?? 200, 1, 500);

                await using var conn = await db.OpenConnectionAsync();
                await using var cmd = conn.CreateCommand();
                cmd.BindByName = true;

                cmd.CommandText = @"
                    SELECT * FROM (
                        SELECT
                            pr.request_id,
                            pr.ticket_id,
                            t.service_no,
                            pr.request_status,
                            pr.note,
                            pr.requested_by,
                            pr.requested_at,
                            pr.created_by,
                            (SELECT COUNT(*)
                               FROM LSTS_PART_REQUEST_ITEMS i
                              WHERE i.request_id = pr.request_id) AS total_items,
                            (SELECT COUNT(*)
                               FROM LSTS_PART_REQUEST_ITEMS i
                              WHERE i.request_id = pr.request_id
                                AND NVL(i.qty_issued, 0) < NVL(i.qty_requested, 0)) AS open_items
                        FROM LSTS_PART_REQUESTS pr
                        JOIN LSTS_TICKETS t ON t.ticket_id = pr.ticket_id
                        WHERE UPPER(pr.request_status) NOT IN ('CLOSED', 'CANCELLED')
                        ORDER BY pr.requested_at DESC NULLS LAST, pr.request_id DESC
                    )
                    WHERE ROWNUM <= :p_limit";

                cmd.Parameters.Add(new OracleParameter("p_limit", OracleDbType.Int32, take, ParameterDirection.Input));

                var rows = new List<object>();
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    rows.Add(new
                    {
                        requestId = ToInt64(r.GetValue(0)),
                        ticketId = ToInt64(r.GetValue(1)),
                        serviceNo = r.IsDBNull(2) ? null : r.GetString(2),
                        requestStatus = r.IsDBNull(3) ? null : r.GetString(3),
                        note = r.IsDBNull(4) ? null : r.GetString(4),
                        requestedBy = ToInt64(r.GetValue(5)),
                        requestedAt = r.IsDBNull(6) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(6),
                        createdBy = ToInt64(r.GetValue(7)),
                        itemCount = ToInt32(r.GetValue(8)),
                        openItemCount = ToInt32(r.GetValue(9)),
                    });
                }

                return Results.Ok(rows);
            })
            .RequireAuthorization("Tickets.Dashboard");

app.MapPost("/api/part-requests", async (
                PartRequestCreateRequest req,
                DbFactory db) =>
            {
                if (req.TicketId <= 0)
                    return Results.BadRequest(new { error = "ticketId must be > 0" });

                if (req.RequestedBy <= 0)
                    return Results.BadRequest(new { error = "requestedBy must be > 0" });

                await using var conn = await db.OpenConnectionAsync();
                using var tx = conn.BeginTransaction();

                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.BindByName = true;

                cmd.CommandText = @"
                    INSERT INTO LSTS_PART_REQUESTS
                    (ticket_id, request_status, note, requested_by, requested_at, created_by)
                    VALUES
                    (:p_ticket_id, 'REQUESTED', :p_note, :p_requested_by, SYSTIMESTAMP, :p_created_by)
                    RETURNING request_id, request_status
                    INTO :o_request_id, :o_request_status";

                cmd.Parameters.Add(new OracleParameter("p_ticket_id", OracleDbType.Int64, req.TicketId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("p_note", OracleDbType.Varchar2, (object?)req.Note ?? DBNull.Value, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("p_requested_by", OracleDbType.Int64, req.RequestedBy, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("p_created_by", OracleDbType.Int64, req.RequestedBy, ParameterDirection.Input));

                var oRequestId = new OracleParameter("o_request_id", OracleDbType.Int64) { Direction = ParameterDirection.Output };
                var oRequestStatus = new OracleParameter("o_request_status", OracleDbType.Varchar2, 20) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(oRequestId);
                cmd.Parameters.Add(oRequestStatus);

                try
                {
                    await cmd.ExecuteNonQueryAsync();
                    tx.Commit();
                }
                catch (OracleException ex) when (ex.Number == 2291)
                {
                    try { tx.Rollback(); } catch { }
                    return Results.BadRequest(new { error = "Invalid foreign key (ticket_id or requested_by not found)" });
                }
                catch (OracleException ex) when (ex.Number == 2290)
                {
                    try { tx.Rollback(); } catch { }
                    return Results.BadRequest(new { error = "DB check constraint failed on part request" });
                }
                catch (OracleException ex) when (ex.Number == 1400)
                {
                    try { tx.Rollback(); } catch { }
                    return Results.BadRequest(new { error = ex.Message });
                }

                var requestId = ToInt64(oRequestId.Value);
                var requestStatus = oRequestStatus.Value is OracleString os ? os.Value : Convert.ToString(oRequestStatus.Value);

                return Results.Created($"/api/part-requests/{requestId}",
                    new PartRequestCreateResponse { RequestId = requestId, RequestStatus = requestStatus ?? "REQUESTED" });
            })
            .RequireAuthorization("PartRequests.Create");

            app.MapGet("/api/part-requests/{requestId:long}", async (long requestId, DbFactory db) =>
            {
                if (requestId <= 0) return Results.BadRequest(new { error = "requestId must be > 0" });

                await using var conn = await db.OpenConnectionAsync();

                await using var headerCmd = conn.CreateCommand();
                headerCmd.BindByName = true;
                headerCmd.CommandText = @"
                    SELECT request_id, ticket_id, request_status, note, requested_by, requested_at, created_by
                    FROM LSTS_PART_REQUESTS
                    WHERE request_id = :p_request_id";
                headerCmd.Parameters.Add(new OracleParameter("p_request_id", OracleDbType.Int64, requestId, ParameterDirection.Input));

                await using var hr = await headerCmd.ExecuteReaderAsync();
                if (!await hr.ReadAsync())
                    return Results.NotFound(new { error = "part request not found" });

                var header = new
                {
                    requestId = ToInt64(hr.GetValue(0)),
                    ticketId = ToInt64(hr.GetValue(1)),
                    requestStatus = hr.IsDBNull(2) ? null : hr.GetString(2),
                    note = hr.IsDBNull(3) ? null : hr.GetString(3),
                    requestedBy = ToInt64(hr.GetValue(4)),
                    requestedAt = hr.GetFieldValue<DateTimeOffset>(5),
                    createdBy = ToInt64(hr.GetValue(6))
                };

                await using var itemsCmd = conn.CreateCommand();
                itemsCmd.BindByName = true;
                itemsCmd.CommandText = @"
                    SELECT i.item_id,
                        i.request_id,
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
                itemsCmd.Parameters.Add(new OracleParameter("p_request_id", OracleDbType.Int64, requestId, ParameterDirection.Input));

                var items = new List<object>();
                await using var ir = await itemsCmd.ExecuteReaderAsync();
                while (await ir.ReadAsync())
                {
                    items.Add(new
                    {
                        itemId = ToInt64(ir.GetValue(0)),
                        requestId = ToInt64(ir.GetValue(1)),
                        partCode = ir.GetString(2),
                        qtyRequested = ToInt32(ir.GetValue(3)),
                        qtyIssued = ToInt32(ir.GetValue(4)),
                        note = ir.IsDBNull(5) ? null : ir.GetString(5),
                        requestedBy = ToInt64(ir.GetValue(6)),
                        requestedAt = ir.GetFieldValue<DateTimeOffset>(7),
                        createdBy = ToInt64(ir.GetValue(8)),
                        createdAt = ir.GetFieldValue<DateTimeOffset>(9)
                    });
                }

                return Results.Ok(new { header, items });
            })
            .RequireAuthorization("Tickets.Dashboard");

            app.MapPost("/api/part-requests/{requestId:long}/items", async (
                long requestId,
                PartRequestItemAddRequest req,
                DbFactory db) =>
            {
                if (requestId <= 0)
                    return Results.BadRequest(new { error = "requestId must be > 0" });

                if (string.IsNullOrWhiteSpace(req.PartCode))
                    return Results.BadRequest(new { error = "partCode is required" });

                if (req.Qty <= 0)
                    return Results.BadRequest(new { error = "qty must be > 0" });

                if (req.RequestedBy <= 0)
                    return Results.BadRequest(new { error = "requestedBy must be > 0" });

                await using var conn = await db.OpenConnectionAsync();
                using var tx = conn.BeginTransaction();


                await using var partCmd = conn.CreateCommand();
                partCmd.Transaction = tx;
                partCmd.BindByName = true;
                partCmd.CommandText = "SELECT part_id FROM LSTS_PARTS WHERE part_code = :p_code";
                partCmd.Parameters.Add(new OracleParameter("p_code", OracleDbType.Varchar2, req.PartCode.Trim(), ParameterDirection.Input));

                var partIdObj = await partCmd.ExecuteScalarAsync();
                if (partIdObj is null)
                {
                    try { tx.Rollback(); } catch { }
                    return Results.BadRequest(new { error = "partCode not found" });
                }

                var partId = ToInt64(partIdObj);

                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.BindByName = true;

                cmd.CommandText = @"
                    INSERT INTO LSTS_PART_REQUEST_ITEMS
                    (request_id, part_id, qty_requested, qty_issued, requested_by, requested_at, created_by, created_at)
                    VALUES
                    (:p_request_id, :p_part_id, :p_qty_requested, 0, :p_requested_by, SYSTIMESTAMP, :p_created_by, SYSTIMESTAMP)
                    RETURNING item_id
                    INTO :o_item_id";

                cmd.Parameters.Add(new OracleParameter("p_request_id", OracleDbType.Int64, requestId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("p_part_id", OracleDbType.Int64, partId, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("p_qty_requested", OracleDbType.Int32, req.Qty, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("p_requested_by", OracleDbType.Int64, req.RequestedBy, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("p_created_by", OracleDbType.Int64, req.RequestedBy, ParameterDirection.Input));

                var oItemId = new OracleParameter("o_item_id", OracleDbType.Int64) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(oItemId);

                try
                {
                    await cmd.ExecuteNonQueryAsync();
                    tx.Commit();
                }
                catch (OracleException ex) when (ex.Number == 2291)
                {
                    try { tx.Rollback(); } catch { }
                    return Results.BadRequest(new { error = "Invalid foreign key (requestId/partId/requestedBy not found)" });
                }
                catch (OracleException ex) when (ex.Number == 1400)
                {
                    try { tx.Rollback(); } catch { }
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (OracleException ex) when (ex.Number == 904)
                {
                    try { tx.Rollback(); } catch { }
                    return Results.Problem(title: "SQL column mismatch", detail: ex.Message, statusCode: 500);
                }

                var itemId = ToInt64(oItemId.Value);

                return Results.Created($"/api/part-requests/{requestId}/items/{itemId}",
                    new PartRequestItemAddResponse
                    {
                        RequestId = requestId,
                        ItemId = itemId,
                        PartCode = req.PartCode,
                        Qty = req.Qty
                    });
            })
            .RequireAuthorization("PartRequests.Create");

            app.MapPatch("/api/part-requests/{requestId:long}/items/{itemId:long}/issue", async (
                long requestId,
                long itemId,
                PartRequestItemIssueRequest req,
                DbFactory db) =>
            {
                if (requestId <= 0) return Results.BadRequest(new { error = "requestId must be > 0" });
                if (itemId <= 0) return Results.BadRequest(new { error = "itemId must be > 0" });
                if (req.IssuedQty <= 0) return Results.BadRequest(new { error = "issuedQty must be > 0" });
                if (req.IssuedBy <= 0) return Results.BadRequest(new { error = "issuedBy must be > 0" });

                await using var conn = await db.OpenConnectionAsync();
                using var tx = conn.BeginTransaction();

                try
                {
                    long ticketId;
                    string reqStatus;

                    await using (var reqCmd = conn.CreateCommand())
                    {
                        reqCmd.Transaction = tx;
                        reqCmd.BindByName = true;
                        reqCmd.CommandText = @"
                            SELECT ticket_id, request_status
                            FROM LSTS_PART_REQUESTS
                            WHERE request_id = :p_request_id
                            FOR UPDATE";

                        reqCmd.Parameters.Add(new OracleParameter("p_request_id", OracleDbType.Int64, requestId, ParameterDirection.Input));

                        await using var rr = await reqCmd.ExecuteReaderAsync();
                        if (!await rr.ReadAsync())
                        {
                            try { tx.Rollback(); } catch { }
                            return Results.NotFound(new { error = "part request not found" });
                        }

                        ticketId = ToInt64(rr.GetValue(0));
                        reqStatus = (rr.IsDBNull(1) ? "REQUESTED" : rr.GetString(1)).Trim().ToUpperInvariant();
                    }

                    if (reqStatus is "CANCELLED" or "CLOSED")
                    {
                        try { tx.Rollback(); } catch { }
                        return Results.BadRequest(new { error = $"cannot issue items when request_status={reqStatus}" });
                    }

                    int qtyRequested, qtyIssued;
                    long partId;

                    await using (var getCmd = conn.CreateCommand())
                    {
                        getCmd.Transaction = tx;
                        getCmd.BindByName = true;
                        getCmd.CommandText = @"
                            SELECT part_id, qty_requested, qty_issued
                            FROM LSTS_PART_REQUEST_ITEMS
                            WHERE request_id = :p_request_id
                            AND item_id    = :p_item_id
                            FOR UPDATE";

                        getCmd.Parameters.Add(new OracleParameter("p_request_id", OracleDbType.Int64, requestId, ParameterDirection.Input));
                        getCmd.Parameters.Add(new OracleParameter("p_item_id", OracleDbType.Int64, itemId, ParameterDirection.Input));

                        await using var rr = await getCmd.ExecuteReaderAsync();
                        if (!await rr.ReadAsync())
                        {
                            try { tx.Rollback(); } catch { }
                            return Results.NotFound(new { error = "item not found for this request" });
                        }

                        partId = ToInt64(rr.GetValue(0));
                        qtyRequested = ToInt32(rr.GetValue(1));
                        qtyIssued = ToInt32(rr.GetValue(2));
                    }

                    var newIssued = qtyIssued + req.IssuedQty;
                    if (newIssued > qtyRequested)
                    {
                        try { tx.Rollback(); } catch { }
                        return Results.BadRequest(new { error = $"issuedQty would exceed qtyRequested ({qtyRequested})" });
                    }

                    int stockQty;
                    decimal unitPrice;

                    await using (var stockGet = conn.CreateCommand())
                    {
                        stockGet.Transaction = tx;
                        stockGet.BindByName = true;
                        stockGet.CommandText = @"
                            SELECT on_hand_qty,
                                NVL(sale_price, 0) AS unit_price
                            FROM LSTS_PARTS
                            WHERE part_id = :p_part_id
                            FOR UPDATE";

                        stockGet.Parameters.Add(new OracleParameter("p_part_id", OracleDbType.Int64, partId, ParameterDirection.Input));

                        await using var r = await stockGet.ExecuteReaderAsync();
                        if (!await r.ReadAsync())
                        {
                            try { tx.Rollback(); } catch { }
                            return Results.BadRequest(new { error = "part not found (part_id invalid)" });
                        }

                        stockQty = ToInt32(r.GetValue(0));
                        unitPrice = ToDecimal(r.GetValue(1));
                    }

                    if (stockQty < req.IssuedQty)
                    {
                        try { tx.Rollback(); } catch { }
                        return Results.BadRequest(new { error = $"insufficient stock. on_hand_qty={stockQty}, requested_issue={req.IssuedQty}" });
                    }

                    await using (var stockUpd = conn.CreateCommand())
                    {
                        stockUpd.Transaction = tx;
                        stockUpd.BindByName = true;
                        stockUpd.CommandText = @"
                            UPDATE LSTS_PARTS
                            SET on_hand_qty = on_hand_qty - :p_dec
                            WHERE part_id = :p_part_id";

                        stockUpd.Parameters.Add(new OracleParameter("p_dec", OracleDbType.Int32, req.IssuedQty, ParameterDirection.Input));
                        stockUpd.Parameters.Add(new OracleParameter("p_part_id", OracleDbType.Int64, partId, ParameterDirection.Input));
                        await stockUpd.ExecuteNonQueryAsync();
                    }

                    await using (var updCmd = conn.CreateCommand())
                    {
                        updCmd.Transaction = tx;
                        updCmd.BindByName = true;
                        updCmd.CommandText = @"
                            UPDATE LSTS_PART_REQUEST_ITEMS
                            SET qty_issued = :p_new_issued
                            WHERE request_id = :p_request_id
                            AND item_id    = :p_item_id";

                        updCmd.Parameters.Add(new OracleParameter("p_new_issued", OracleDbType.Int32, newIssued, ParameterDirection.Input));
                        updCmd.Parameters.Add(new OracleParameter("p_request_id", OracleDbType.Int64, requestId, ParameterDirection.Input));
                        updCmd.Parameters.Add(new OracleParameter("p_item_id", OracleDbType.Int64, itemId, ParameterDirection.Input));
                        await updCmd.ExecuteNonQueryAsync();
                    }

                    await using (var usageCmd = conn.CreateCommand())
                    {
                        usageCmd.Transaction = tx;
                        usageCmd.BindByName = true;

                        usageCmd.CommandText = @"
                            INSERT INTO LSTS_TICKET_PART_USAGE
                            (ticket_id, part_id, qty, unit_price, note, created_at, created_by)
                            VALUES
                            (:p_ticket_id, :p_part_id, :p_qty, :p_unit_price, :p_note, SYSTIMESTAMP, :p_created_by)";

                        usageCmd.Parameters.Add(new OracleParameter("p_ticket_id", OracleDbType.Int64, ticketId, ParameterDirection.Input));
                        usageCmd.Parameters.Add(new OracleParameter("p_part_id", OracleDbType.Int64, partId, ParameterDirection.Input));
                        usageCmd.Parameters.Add(new OracleParameter("p_qty", OracleDbType.Int32, req.IssuedQty, ParameterDirection.Input));
                        usageCmd.Parameters.Add(new OracleParameter("p_unit_price", OracleDbType.Decimal, unitPrice, ParameterDirection.Input));

                        usageCmd.Parameters.Add(new OracleParameter("p_note", OracleDbType.Varchar2,
                            (object)$"Issued from part request #{requestId}, item #{itemId}",
                            ParameterDirection.Input));

                        usageCmd.Parameters.Add(new OracleParameter("p_created_by", OracleDbType.Int64, req.IssuedBy, ParameterDirection.Input));

                        await usageCmd.ExecuteNonQueryAsync();
                    }

                    int totalItems, fullIssued, anyIssued;

                    await using (var statCmd = conn.CreateCommand())
                    {
                        statCmd.Transaction = tx;
                        statCmd.BindByName = true;
                        statCmd.CommandText = @"
                            SELECT COUNT(*) total_items,
                                SUM(CASE WHEN qty_issued >= qty_requested THEN 1 ELSE 0 END) full_issued,
                                SUM(CASE WHEN qty_issued > 0 THEN 1 ELSE 0 END) any_issued
                            FROM LSTS_PART_REQUEST_ITEMS
                            WHERE request_id = :p_request_id";

                        statCmd.Parameters.Add(new OracleParameter("p_request_id", OracleDbType.Int64, requestId, ParameterDirection.Input));

                        await using var sr = await statCmd.ExecuteReaderAsync();
                        await sr.ReadAsync();

                        totalItems = ToInt32(sr.GetValue(0));
                        fullIssued = ToInt32(sr.GetValue(1));
                        anyIssued = ToInt32(sr.GetValue(2));
                    }

                    var newStatus =
                        (totalItems > 0 && fullIssued == totalItems) ? "ISSUED" :
                        (anyIssued > 0) ? "PARTIALLY_ISSUED" :
                        "REQUESTED";

                    await using (var updHdr = conn.CreateCommand())
                    {
                        updHdr.Transaction = tx;
                        updHdr.BindByName = true;
                        updHdr.CommandText = @"
                            UPDATE LSTS_PART_REQUESTS
                            SET request_status = :p_status
                            WHERE request_id = :p_request_id";

                        updHdr.Parameters.Add(new OracleParameter("p_status", OracleDbType.Varchar2, newStatus, ParameterDirection.Input));
                        updHdr.Parameters.Add(new OracleParameter("p_request_id", OracleDbType.Int64, requestId, ParameterDirection.Input));
                        await updHdr.ExecuteNonQueryAsync();
                    }

                    tx.Commit();

                    return Results.Ok(new
                    {
                        requestId,
                        itemId,
                        ticketId,
                        partId,
                        qtyRequested,
                        qtyIssuedOld = qtyIssued,
                        qtyIssuedNew = newIssued,
                        stockQtyOld = stockQty,
                        stockQtyNew = stockQty - req.IssuedQty,
                        requestStatus = newStatus
                    });
                }
                catch
                {
                    try { tx.Rollback(); } catch { }
                    throw;
                }
            })
            .RequireAuthorization("PartRequests.Issue");

            app.MapPatch("/api/part-requests/{requestId:long}/status", async (
                long requestId,
                PartRequestStatusUpdateRequest req,
                DbFactory db) =>
            {
                return await CallPartRequestStatusUpdate(requestId, req, db);
            })
            .RequireAuthorization("PartRequests.Manage");

            app.MapPatch("/api/part-requests/{requestId:long}/cancel", async (
                long requestId,
                PartRequestCancelRequest req,
                DbFactory db) =>
            {
                var statusReq = new PartRequestStatusUpdateRequest("CANCELLED", req.CancelledBy, req.Note);
                return await CallPartRequestStatusUpdate(requestId, statusReq, db);
            })
            .RequireAuthorization("PartRequests.Manage");

            app.MapPatch("/api/part-requests/{requestId:long}/close", async (
                long requestId,
                PartRequestCloseRequest req,
                DbFactory db) =>
            {
                var statusReq = new PartRequestStatusUpdateRequest("CLOSED", req.ClosedBy, req.Note);
                return await CallPartRequestStatusUpdate(requestId, statusReq, db);
            })
            .RequireAuthorization("PartRequests.Manage");

            static bool IsValidPartRequestStatus(string s) =>
                s is "REQUESTED" or "PARTIALLY_ISSUED" or "ISSUED" or "CANCELLED" or "CLOSED";

            static async Task<IResult> CallPartRequestStatusUpdate(long requestId, PartRequestStatusUpdateRequest req, DbFactory db)
            {
                if (requestId <= 0) return Results.BadRequest(new { error = "requestId must be > 0" });
                if (string.IsNullOrWhiteSpace(req.ToStatus)) return Results.BadRequest(new { error = "toStatus is required" });
                if (req.ChangedBy <= 0) return Results.BadRequest(new { error = "changedBy must be > 0" });

                var toStatus = req.ToStatus.Trim().ToUpperInvariant();

                if (!IsValidPartRequestStatus(toStatus))
                    return Results.BadRequest(new { error = "invalid status" });

                await using var conn = await db.OpenConnectionAsync();
                using var tx = conn.BeginTransaction();

                try
                {
                    string currentStatus;

                    await using (var lockCmd = conn.CreateCommand())
                    {
                        lockCmd.Transaction = tx;
                        lockCmd.BindByName = true;
                        lockCmd.CommandText = @"
                            SELECT request_status
                            FROM LSTS_PART_REQUESTS
                            WHERE request_id = :p_id
                            FOR UPDATE";

                        lockCmd.Parameters.Add(new OracleParameter("p_id", OracleDbType.Int64, requestId, ParameterDirection.Input));

                        var obj = await lockCmd.ExecuteScalarAsync();
                        if (obj is null)
                        {
                            try { tx.Rollback(); } catch { }
                            return Results.NotFound(new { error = "part request not found" });
                        }

                        currentStatus = Convert.ToString(obj) ?? "REQUESTED";
                        currentStatus = currentStatus.Trim().ToUpperInvariant();
                    }

                    if (toStatus == "CLOSED" && currentStatus != "ISSUED")
                    {
                        try { tx.Rollback(); } catch { }
                        return Results.BadRequest(new { error = "can only CLOSE an ISSUED request" });
                    }

                    if (toStatus == "CANCELLED")
                    {
                        int anyIssued;
                        await using (var chkCmd = conn.CreateCommand())
                        {
                            chkCmd.Transaction = tx;
                            chkCmd.BindByName = true;
                            chkCmd.CommandText = @"
                                SELECT SUM(CASE WHEN qty_issued > 0 THEN 1 ELSE 0 END)
                                FROM LSTS_PART_REQUEST_ITEMS
                                WHERE request_id = :p_id";

                            chkCmd.Parameters.Add(new OracleParameter("p_id", OracleDbType.Int64, requestId, ParameterDirection.Input));

                            var v = await chkCmd.ExecuteScalarAsync();
                            anyIssued = v is null || v is DBNull ? 0 : ToInt32(v);
                        }

                        if (anyIssued > 0)
                        {
                            try { tx.Rollback(); } catch { }
                            return Results.BadRequest(new { error = "cannot CANCEL: some items already issued" });
                        }
                    }
                    
                    await using (var updCmd = conn.CreateCommand())
                    {
                        updCmd.Transaction = tx;
                        updCmd.BindByName = true;

                        updCmd.CommandText = @"
                            UPDATE LSTS_PART_REQUESTS
                            SET request_status = :p_status,
                                note = COALESCE(:p_note, note)
                            WHERE request_id = :p_id";

                        updCmd.Parameters.Add(new OracleParameter("p_status", OracleDbType.Varchar2, toStatus, ParameterDirection.Input));
                        updCmd.Parameters.Add(new OracleParameter("p_note", OracleDbType.Varchar2, (object?)req.Note ?? DBNull.Value, ParameterDirection.Input));
                        updCmd.Parameters.Add(new OracleParameter("p_id", OracleDbType.Int64, requestId, ParameterDirection.Input));

                        await updCmd.ExecuteNonQueryAsync();
                    }

                    tx.Commit();
                    return Results.Ok(new PartRequestStatusUpdateResponse(requestId, toStatus));
                }
                catch
                {
                    try { tx.Rollback(); } catch { }
                    throw;
                }
            }

            return app;
        }
    }