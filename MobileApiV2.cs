using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GiantGroupM3
{
    /// <summary>
    /// Extra endpoints for the full-feature Giant Group app ("Giant Group Ops Plus").
    /// Plugged into MobileApi.ashx so ONE handler serves both apps:
    ///   - the original app keeps using requests / jobs / photos / dashboard exactly as before (untouched);
    ///   - the new app additionally uses calendar, quotes, allocations, clients, payments, reports and lookups.
    /// Like the rest of MobileApi there is no sign-in: every caller is treated as management.
    /// Written for C# 7.3 / .NET Framework 4.7.2.
    /// </summary>
    public static class MobileApiV2
    {
        private static readonly string[] QuoteStatuses = { "Pending", "Sent", "Accepted", "Declined", "Rejected" };
        private static readonly string[] Urgencies = { "High", "Medium", "Low", "Not Specified" };
        private static readonly string[] ClientTypes = { "Residential", "Commercial" };

        // Same constants as RequestQuote.aspx.cs so a quote made on the phone equals one made on the website.
        private const double BaseLatitude = -29.890840081918007;
        private const double BaseLongitude = 30.905937134956915;
        private const decimal CostPerKilometer = 36.00m;
        private const decimal BaseCallOutFee = 500.00m;
        private const decimal FlatTravelFee = 850.00m;
        private const decimal VatRate = 0.15m;

        private class V2Error : Exception { public int Code; public V2Error(int code, string msg) : base(msg) { Code = code; } }

        // ================================================================== entry
        /// <summary>Returns true when the route belonged to the new app (response written), false to let the caller 404.</summary>
        public static bool TryHandle(HttpContext ctx, string[] p, string m, string conn)
        {
            try
            {
                return Route(ctx, p, m, conn);
            }
            catch (V2Error ex) { Fail(ctx, ex.Code, ex.Message); return true; }
            catch (SqlException ex)
            {
                System.Diagnostics.Trace.TraceError("MobileApiV2 SQL error: " + ex);
                Fail(ctx, 500, "The database could not complete the request.");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("MobileApiV2 failed: " + ex);
                Fail(ctx, 500, "The server could not complete the request.");
                return true;
            }
        }

        private static bool Route(HttpContext ctx, string[] p, string m, string conn)
        {
            if (p.Length == 0) return false;
            int id;
            switch (p[0])
            {
                case "overview":
                    if (p.Length == 1 && m == "GET") { Overview(ctx, conn); return true; }
                    break;
                case "calendar":
                    if (p.Length == 1 && m == "GET") { Calendar(ctx, conn); return true; }
                    break;
                case "lookups":
                    if (p.Length == 1 && m == "GET") { Lookups(ctx, conn); return true; }
                    break;
                case "reports":
                    if (p.Length == 2 && p[1] == "summary" && m == "GET") { Report(ctx, conn); return true; }
                    break;

                case "clients":
                    if (p.Length == 1 && m == "GET") { ListClients(ctx, conn); return true; }
                    if (p.Length == 1 && m == "POST") { SaveClient(ctx, conn, 0); return true; }
                    if (p.Length == 2 && int.TryParse(p[1], out id))
                    {
                        if (m == "GET") { GetClient(ctx, conn, id); return true; }
                        if (m == "POST") { SaveClient(ctx, conn, id); return true; }
                    }
                    break;

                case "requests":
                    if (p.Length == 1 && m == "POST") { CreateRequest(ctx, conn); return true; }
                    if (p.Length == 3 && int.TryParse(p[1], out id) && p[2] == "evaluation-date" && m == "POST") { SetEvaluationDate(ctx, conn, id); return true; }
                    break;

                case "quotes":
                    if (p.Length == 1 && m == "GET") { ListQuotes(ctx, conn); return true; }
                    if (p.Length == 1 && m == "POST") { CreateQuote(ctx, conn); return true; }
                    if (p.Length == 2 && p[1] == "preview" && m == "GET") { QuotePreview(ctx, conn); return true; }
                    if (p.Length == 2 && int.TryParse(p[1], out id))
                    {
                        if (m == "GET") { GetQuote(ctx, conn, id); return true; }
                        if (m == "POST") { UpdateQuote(ctx, conn, id); return true; }
                    }
                    break;

                case "jobs":
                    if (p.Length == 3 && int.TryParse(p[1], out id))
                    {
                        if (p[2] == "allocations" && m == "GET") { JobAllocations(ctx, conn, id); return true; }
                        if (p[2] == "options" && m == "GET") { AssignOptions(ctx, conn, id); return true; }
                        if (p[2] == "assign-staff" && m == "POST") { AssignStaff(ctx, conn, id); return true; }
                        if (p[2] == "assign-asset" && m == "POST") { AssignAsset(ctx, conn, id); return true; }
                    }
                    break;

                case "allocations":
                    if (p.Length == 2 && p[1] == "staff" && m == "GET") { StaffAllocations(ctx, conn); return true; }
                    if (p.Length == 2 && p[1] == "assets" && m == "GET") { AssetAllocations(ctx, conn); return true; }
                    if (p.Length == 3 && p[1] == "staff" && p[2] == "unassign" && m == "POST") { UnassignStaff(ctx, conn); return true; }
                    if (p.Length == 3 && p[1] == "assets" && p[2] == "unassign" && m == "POST") { UnassignAsset(ctx, conn); return true; }
                    break;

                case "payments":
                    if (p.Length == 1 && m == "GET") { ListPayments(ctx, conn); return true; }
                    if (p.Length == 3 && p[1] == "job" && int.TryParse(p[2], out id))
                    {
                        if (m == "GET") { JobPayments(ctx, conn, id); return true; }
                        if (m == "POST") { RecordPayment(ctx, conn, id); return true; }
                    }
                    break;
            }
            return false;
        }

        // ================================================================== overview / lookups
        private static void Overview(HttpContext ctx, string conn)
        {
            var o = new JObject();
            using (var c = new SqlConnection(conn))
            {
                c.Open();
                using (var cmd = new SqlCommand(@"SELECT
                    (SELECT COUNT(*) FROM dbo.Quote WHERE quoteStatus = 'Sent') AS quotesSent,
                    (SELECT COUNT(*) FROM dbo.Quote WHERE quoteStatus = 'Accepted') AS quotesAccepted,
                    (SELECT COUNT(*) FROM dbo.JobRequest WHERE status = 'Site Evaluated') AS readyToQuote,
                    (SELECT COUNT(*) FROM dbo.Job j WHERE ISNULL(j.jobStatus,'') NOT IN ('Archived','Completed')
                        AND NOT EXISTS (SELECT 1 FROM dbo.JobStaffAssignment a WHERE a.jobID = j.jobID)) AS jobsWithoutCrew,
                    (SELECT COUNT(*) FROM dbo.Client) AS clients", c))
                using (var r = cmd.ExecuteReader())
                {
                    r.Read();
                    foreach (string k in new[] { "quotesSent", "quotesAccepted", "readyToQuote", "jobsWithoutCrew", "clients" })
                        o[k] = Convert.ToInt32(r[k]);
                }
            }
            decimal invoiced, paid;
            Totals(conn, out invoiced, out paid);
            o["outstanding"] = invoiced - paid;
            Json(ctx, o);
        }

        private static void Totals(string conn, out decimal invoiced, out decimal paid)
        {
            invoiced = 0; paid = 0;
            using (var c = new SqlConnection(conn))
            using (var cmd = new SqlCommand(@"SELECT
                ISNULL((SELECT SUM(q.amount) FROM dbo.Job j INNER JOIN dbo.Quote q ON q.QuoteID = j.quoteID WHERE ISNULL(j.jobStatus,'') <> 'Archived'), 0),
                ISNULL((SELECT SUM(p.amountPaid) FROM dbo.Payment p), 0)", c))
            {
                c.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read()) { invoiced = Convert.ToDecimal(r[0]); paid = Convert.ToDecimal(r[1]); }
                }
            }
        }

        private static void Lookups(HttpContext ctx, string conn)
        {
            var o = new JObject();
            var slots = new JArray();
            DataTable ts = Scheduling.GetTimeSlots();
            foreach (DataRow r in ts.Rows)
                slots.Add(new JObject { { "id", Convert.ToInt32(r["timeSlotID"]) }, { "label", Convert.ToString(r["Label"]) } });
            o["timeSlots"] = slots;
            o["jobTypes"] = JArray.FromObject(Rows(conn, "SELECT jobTypeID AS id, jobTypeName AS name, JobRate AS rate FROM dbo.JobType ORDER BY jobTypeName", 100));
            o["urgencies"] = JArray.FromObject(Urgencies);
            o["clientTypes"] = JArray.FromObject(ClientTypes);
            o["quoteStatuses"] = JArray.FromObject(QuoteStatuses);
            Json(ctx, o);
        }

        // ================================================================== calendar
        private static void Calendar(HttpContext ctx, string conn)
        {
            DateTime from = ParseDate(ctx.Request.QueryString["from"], DateTime.Today.AddDays(-DateTime.Today.Day + 1));
            DateTime to = ParseDate(ctx.Request.QueryString["to"], from.AddMonths(1));
            if (to <= from) to = from.AddDays(1);
            if ((to - from).TotalDays > 400) throw new V2Error(400, "Choose a shorter date range.");

            var events = new List<JObject>();

            foreach (var r in Rows(conn, @"
                SELECT j.jobID, j.jobStatus, j.startDate, jr.jobRequestID, jr.siteAddress, jr.latitude, jr.longitude, jr.urgencyLevel,
                       LTRIM(ISNULL(c.clientName, '') + ' ' + ISNULL(c.clientSurname, '')) AS clientName, c.phoneNumber AS clientPhone,
                       STUFF((SELECT ', ' + jt.jobTypeName FROM dbo.RequestItem ri INNER JOIN dbo.JobType jt ON jt.jobTypeID = ri.jobTypeID
                              WHERE ri.jobRequestID = jr.jobRequestID ORDER BY jt.jobTypeName FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS items,
                       STUFF((SELECT ', ' + LTRIM(ISNULL(st.firstName, '') + ' ' + ISNULL(st.lastName, '')) FROM dbo.JobStaffAssignment ja2
                              INNER JOIN dbo.Staff st ON st.staffID = ja2.staffID WHERE ja2.jobID = j.jobID ORDER BY st.firstName
                              FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS crew
                FROM dbo.Job j
                LEFT JOIN dbo.Quote q ON q.QuoteID = j.quoteID
                LEFT JOIN dbo.JobRequest jr ON jr.jobRequestID = q.jobRequestID
                LEFT JOIN dbo.Client c ON c.clientID = jr.clientID
                WHERE j.startDate IS NOT NULL AND j.startDate >= @from AND j.startDate < @to AND ISNULL(j.jobStatus, '') <> 'Archived'
                ORDER BY j.startDate", 1000, P("@from", SqlDbType.DateTime, from), P("@to", SqlDbType.DateTime, to)))
            {
                events.Add(new JObject {
                    { "kind", "job" }, { "id", Convert.ToInt32(r["jobID"]) }, { "requestId", r["jobRequestID"] == null ? (JToken)null : Convert.ToInt32(r["jobRequestID"]) },
                    { "date", Convert.ToDateTime(r["startDate"]).Date }, { "status", Str(r["jobStatus"]) }, { "address", Str(r["siteAddress"]) },
                    { "latitude", Dbl(r["latitude"]) }, { "longitude", Dbl(r["longitude"]) }, { "urgency", Str(r["urgencyLevel"]) },
                    { "clientName", Str(r["clientName"]) }, { "clientPhone", Str(r["clientPhone"]) }, { "items", Str(r["items"]) }, { "crew", Str(r["crew"]) } });
            }

            foreach (var r in Rows(conn, @"
                SELECT jr.jobRequestID, jr.siteEvaluationDate, jr.status, jr.siteAddress, jr.latitude, jr.longitude, jr.urgencyLevel,
                       LTRIM(ISNULL(c.clientName, '') + ' ' + ISNULL(c.clientSurname, '')) AS clientName, c.phoneNumber AS clientPhone,
                       STUFF((SELECT ', ' + jt.jobTypeName FROM dbo.RequestItem ri INNER JOIN dbo.JobType jt ON jt.jobTypeID = ri.jobTypeID
                              WHERE ri.jobRequestID = jr.jobRequestID ORDER BY jt.jobTypeName FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS items
                FROM dbo.JobRequest jr LEFT JOIN dbo.Client c ON c.clientID = jr.clientID
                WHERE jr.siteEvaluationDate IS NOT NULL AND jr.siteEvaluationDate >= @from AND jr.siteEvaluationDate < @to AND ISNULL(jr.status, '') <> 'Cancelled'
                ORDER BY jr.siteEvaluationDate", 1000, P("@from", SqlDbType.DateTime, from), P("@to", SqlDbType.DateTime, to)))
            {
                events.Add(new JObject {
                    { "kind", "evaluation" }, { "id", Convert.ToInt32(r["jobRequestID"]) }, { "requestId", Convert.ToInt32(r["jobRequestID"]) },
                    { "date", Convert.ToDateTime(r["siteEvaluationDate"]).Date }, { "status", Str(r["status"]) }, { "address", Str(r["siteAddress"]) },
                    { "latitude", Dbl(r["latitude"]) }, { "longitude", Dbl(r["longitude"]) }, { "urgency", Str(r["urgencyLevel"]) },
                    { "clientName", Str(r["clientName"]) }, { "clientPhone", Str(r["clientPhone"]) }, { "items", Str(r["items"]) }, { "crew", null } });
            }
            Json(ctx, new JArray(events.OrderBy(e => (DateTime)e["date"]).ThenBy(e => (string)e["kind"])));
        }

        // ================================================================== clients
        private static void ListClients(HttpContext ctx, string conn)
        {
            string search = (ctx.Request.QueryString["search"] ?? "").Trim();
            string sql = @"SELECT c.clientID, c.clientName AS firstName, c.clientSurname AS surname, LTRIM(c.clientName + ' ' + ISNULL(c.clientSurname,'')) AS fullName,
                c.clientType, c.emailAddress AS email, c.phoneNumber AS phone, c.status, c.dateAdded,
                (SELECT COUNT(*) FROM dbo.JobRequest jr WHERE jr.clientID = c.clientID) AS requestCount
                FROM dbo.Client c WHERE 1=1";
            var ps = new List<SqlParameter>();
            if (search.Length > 0)
            {
                sql += " AND (c.clientName LIKE @s OR c.clientSurname LIKE @s OR c.emailAddress LIKE @s OR c.phoneNumber LIKE @s OR CONVERT(varchar(12), c.clientID) = @e)";
                ps.Add(P("@s", SqlDbType.VarChar, Like(search))); ps.Add(P("@e", SqlDbType.VarChar, search));
            }
            sql += " ORDER BY c.clientName, c.clientSurname";
            Json(ctx, Rows(conn, sql, 300, ps.ToArray()));
        }

        private static JObject LoadClient(string conn, int id)
        {
            var rows = Rows(conn, @"SELECT c.clientID, c.clientName AS firstName, c.clientSurname AS surname, LTRIM(c.clientName + ' ' + ISNULL(c.clientSurname,'')) AS fullName,
                c.clientType, c.emailAddress AS email, c.phoneNumber AS phone, c.status, c.dateAdded,
                (SELECT COUNT(*) FROM dbo.JobRequest jr WHERE jr.clientID = c.clientID) AS requestCount
                FROM dbo.Client c WHERE c.clientID = @id", 1, P("@id", SqlDbType.Int, id));
            if (rows.Count == 0) throw new V2Error(404, "Client not found.");
            var o = JObject.FromObject(rows[0]);
            o["requests"] = JArray.FromObject(Rows(conn, @"SELECT jr.jobRequestID, jr.status, jr.siteAddress, jr.dateRecieved AS dateReceived FROM dbo.JobRequest jr
                WHERE jr.clientID = @id ORDER BY jr.dateRecieved DESC, jr.jobRequestID DESC", 50, P("@id", SqlDbType.Int, id)));
            return o;
        }

        private static void GetClient(HttpContext ctx, string conn, int id) { Json(ctx, LoadClient(conn, id)); }

        private static void SaveClient(HttpContext ctx, string conn, int id)
        {
            JObject b = Body(ctx);
            string first = ((string)b["firstName"] ?? "").Trim(), last = ((string)b["surname"] ?? "").Trim();
            string email = ((string)b["email"] ?? "").Trim(), phone = ((string)b["phone"] ?? "").Trim();
            string type = ((string)b["clientType"] ?? "Residential").Trim();
            if (first.Length == 0 || first.Length > 60) throw new V2Error(400, "Enter the client's first name.");
            if (last.Length == 0 || last.Length > 60) throw new V2Error(400, "Enter the client's surname.");
            string canonType = ClientTypes.FirstOrDefault(t => t.Equals(type, StringComparison.OrdinalIgnoreCase));
            if (canonType == null) throw new V2Error(400, "Client type must be Residential or Commercial.");
            if (email.Length > 0 && (email.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")))
                throw new V2Error(400, "Enter a valid email address.");
            if (phone.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[0-9+\-\s()]{7,20}$"))
                throw new V2Error(400, "Enter a valid phone number, for example 0821234567.");

            using (var c = new SqlConnection(conn))
            {
                c.Open();
                if (email.Length > 0)
                {
                    using (var dup = new SqlCommand(@"SELECT (SELECT COUNT(*) FROM dbo.Client WHERE emailAddress = @e AND clientID <> @id)
                                                          + (SELECT COUNT(*) FROM dbo.Staff WHERE emailAddress = @e)", c))
                    {
                        dup.Parameters.Add("@e", SqlDbType.VarChar, 100).Value = email;
                        dup.Parameters.Add("@id", SqlDbType.Int).Value = id;
                        if (Convert.ToInt32(dup.ExecuteScalar()) > 0) throw new V2Error(409, "Another client or staff member already uses that email address.");
                    }
                }
                if (id == 0)
                {
                    using (var cmd = new SqlCommand(@"INSERT INTO dbo.Client (clientName, clientSurname, clientType, emailAddress, status, dateAdded, phoneNumber)
                        VALUES (@f, @l, @t, NULLIF(@e,''), 'Active', GETDATE(), NULLIF(@p,'')); SELECT CAST(SCOPE_IDENTITY() AS int);", c))
                    {
                        AddClientParams(cmd, first, last, canonType, email, phone);
                        id = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                else
                {
                    using (var cmd = new SqlCommand(@"UPDATE dbo.Client SET clientName=@f, clientSurname=@l, clientType=@t, emailAddress=NULLIF(@e,''), phoneNumber=NULLIF(@p,'')
                        WHERE clientID=@id", c))
                    {
                        AddClientParams(cmd, first, last, canonType, email, phone);
                        cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                        if (cmd.ExecuteNonQuery() != 1) throw new V2Error(404, "Client not found.");
                    }
                }
            }
            Json(ctx, LoadClient(conn, id));
        }

        private static void AddClientParams(SqlCommand cmd, string f, string l, string t, string e, string p)
        {
            cmd.Parameters.Add("@f", SqlDbType.VarChar, 60).Value = f;
            cmd.Parameters.Add("@l", SqlDbType.VarChar, 60).Value = l;
            cmd.Parameters.Add("@t", SqlDbType.VarChar, 20).Value = t;
            cmd.Parameters.Add("@e", SqlDbType.VarChar, 100).Value = e;
            cmd.Parameters.Add("@p", SqlDbType.VarChar, 20).Value = p;
        }

        // ================================================================== requests (create / evaluation date)
        private static void CreateRequest(HttpContext ctx, string conn)
        {
            JObject b = Body(ctx);
            int clientId = (int?)b["clientId"] ?? 0;
            string address = ((string)b["siteAddress"] ?? "").Trim();
            string urgency = ((string)b["urgency"] ?? "").Trim();
            decimal lat, lng;
            if (!TryDecimal(b["latitude"], out lat) || !TryDecimal(b["longitude"], out lng) || lat < -90m || lat > 90m || lng < -180m || lng > 180m)
                throw new V2Error(400, "Pin the site location on the map first.");
            if (address.Length == 0 || address.Length > 100) throw new V2Error(400, "Enter a site address of no more than 100 characters.");
            string canonUrgency = Urgencies.FirstOrDefault(u => u.Equals(urgency, StringComparison.OrdinalIgnoreCase));
            if (canonUrgency == null) throw new V2Error(400, "Choose an urgency.");
            var typeIds = new List<int>();
            JArray arr = b["jobTypeIds"] as JArray;
            if (arr != null) foreach (var t in arr) typeIds.Add((int)t);
            typeIds = typeIds.Distinct().ToList();
            if (typeIds.Count == 0) throw new V2Error(400, "Choose at least one service.");

            int newId;
            using (var c = new SqlConnection(conn))
            {
                c.Open();
                using (var chk = new SqlCommand("SELECT COUNT(*) FROM dbo.Client WHERE clientID = @id", c))
                {
                    chk.Parameters.Add("@id", SqlDbType.Int).Value = clientId;
                    if (Convert.ToInt32(chk.ExecuteScalar()) == 0) throw new V2Error(400, "Choose the client this request is for.");
                }
                using (var tx = c.BeginTransaction())
                {
                    try
                    {
                        newId = InsertRequest(c, tx, clientId, address, lat, lng, canonUrgency, typeIds);
                        tx.Commit();
                    }
                    catch (Exception) { try { tx.Rollback(); } catch (Exception) { } throw; }
                }
            }
            ctx.Response.StatusCode = 201;
            Json(ctx, new JObject { { "jobRequestId", newId } });
        }

        private static int InsertRequest(SqlConnection c, SqlTransaction tx, int clientId, string address, decimal lat, decimal lng, string urgency, List<int> typeIds)
        {
            // The source column may carry a CHECK constraint listing allowed values; fall back to the website's value if so.
            string[] sources = { "Mobile App", "Website" };
            for (int i = 0; i < sources.Length; i++)
            {
                try
                {
                    // A failed statement inside a transaction can poison it, so use a savepoint per attempt.
                    tx.Save("ins" + i);
                    int id;
                    using (var cmd = new SqlCommand(@"INSERT INTO dbo.JobRequest (clientID, siteAddress, longitude, latitude, requestSource, urgencyLevel)
                        VALUES (@c, @a, @lng, @lat, @src, @u); SELECT CAST(SCOPE_IDENTITY() AS int);", c, tx))
                    {
                        cmd.Parameters.Add("@c", SqlDbType.Int).Value = clientId;
                        cmd.Parameters.Add("@a", SqlDbType.VarChar, 100).Value = address;
                        var plng = cmd.Parameters.Add("@lng", SqlDbType.Decimal); plng.Precision = 9; plng.Scale = 6; plng.Value = lng;
                        var plat = cmd.Parameters.Add("@lat", SqlDbType.Decimal); plat.Precision = 9; plat.Scale = 6; plat.Value = lat;
                        cmd.Parameters.Add("@src", SqlDbType.VarChar, 20).Value = sources[i];
                        cmd.Parameters.Add("@u", SqlDbType.VarChar, 15).Value = urgency;
                        id = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    foreach (int t in typeIds)
                    {
                        using (var it = new SqlCommand("INSERT INTO dbo.RequestItem (jobRequestID, jobTypeID) SELECT @r, jobTypeID FROM dbo.JobType WHERE jobTypeID = @t", c, tx))
                        {
                            it.Parameters.Add("@r", SqlDbType.Int).Value = id;
                            it.Parameters.Add("@t", SqlDbType.Int).Value = t;
                            it.ExecuteNonQuery();
                        }
                    }
                    return id;
                }
                catch (SqlException ex) when (ex.Number == 547 && i < sources.Length - 1)
                {
                    tx.Rollback("ins" + i);
                }
            }
            throw new V2Error(500, "The request could not be saved.");
        }

        private static void SetEvaluationDate(HttpContext ctx, string conn, int id)
        {
            JObject b = Body(ctx);
            DateTime? d = null;
            string s = (string)b["date"];
            DateTime parsed;
            if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)) d = parsed.Date;
            if (d == null) throw new V2Error(400, "Choose an evaluation date.");
            using (var c = new SqlConnection(conn))
            using (var cmd = new SqlCommand(@"UPDATE dbo.JobRequest SET siteEvaluationDate = @d,
                    status = CASE WHEN status = 'Pending' THEN 'Evaluation Date Set' ELSE status END
                    WHERE jobRequestID = @id AND ISNULL(status,'') <> 'Cancelled'", c))
            {
                cmd.Parameters.Add("@d", SqlDbType.Date).Value = d.Value;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                c.Open();
                if (cmd.ExecuteNonQuery() != 1) throw new V2Error(409, "That request was not found or has been cancelled.");
            }
            Json(ctx, new JObject { { "ok", true }, { "date", d.Value } });
        }

        // ================================================================== quotes
        private const string BillableCase = @"CASE
                    WHEN jt.JobTypeName = 'Tree Felling' THEN 1
                    WHEN jt.JobTypeName = 'Grass Cutting' THEN 6
                    WHEN jt.JobTypeName = 'Tree Planting' THEN 10
                    WHEN jt.JobTypeName = 'Vegetation Clearance' THEN 14
                    WHEN jt.JobTypeName = 'Hedge Trimming' THEN 18
                    ELSE NULL END";

        private class Figures { public decimal Services, Travel, Subtotal, Vat, Total; public double OneWayKm; public string TravelNote; public List<Dictionary<string, object>> Lines; }

        private static Figures Calculate(string conn, int requestId)
        {
            var f = new Figures();
            f.Lines = Rows(conn, @"SELECT jt.JobTypeName AS name, jt.JobRate AS rate, CAST(ISNULL(b.Quantity, 0) AS decimal(18,2)) AS quantity,
                    CAST(jt.JobRate * ISNULL(b.Quantity, 0) AS decimal(18,2)) AS lineTotal
                FROM dbo.RequestItem ri INNER JOIN dbo.JobType jt ON ri.jobTypeID = jt.jobTypeID
                OUTER APPLY (SELECT TOP (1) TRY_CONVERT(DECIMAL(18,2), id.detailValue) AS Quantity FROM dbo.ItemDetail id
                             WHERE id.requestItemID = ri.requestItemID AND id.jobDetailID = " + BillableCase + @") b
                WHERE ri.jobRequestID = @id ORDER BY jt.JobTypeName", 50, P("@id", SqlDbType.Int, requestId));
            decimal services = 0;
            foreach (var l in f.Lines) services += Convert.ToDecimal(l["lineTotal"]);
            f.Services = Math.Round(services, 2, MidpointRounding.AwayFromZero);

            var job = Rows(conn, "SELECT latitude, longitude FROM dbo.JobRequest WHERE jobRequestID = @id", 1, P("@id", SqlDbType.Int, requestId));
            double lat = job.Count > 0 && job[0]["latitude"] != null ? Convert.ToDouble(job[0]["latitude"]) : 0.0;
            double lng = job.Count > 0 && job[0]["longitude"] != null ? Convert.ToDouble(job[0]["longitude"]) : 0.0;
            if (lat == 0.0 || lng == 0.0)
            {
                f.OneWayKm = 0; f.Travel = FlatTravelFee;
                f.TravelNote = "Standard flat-rate travel fee (no site coordinates).";
            }
            else
            {
                double dLat = (lat - BaseLatitude) * (Math.PI / 180.0), dLng = (lng - BaseLongitude) * (Math.PI / 180.0);
                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(BaseLatitude * (Math.PI / 180.0)) * Math.Cos(lat * (Math.PI / 180.0)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
                double oneWay = 6371.0 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                decimal roundTrip = Convert.ToDecimal(oneWay) * 2;
                f.OneWayKm = oneWay;
                f.Travel = Math.Round(BaseCallOutFee + roundTrip * CostPerKilometer, 2, MidpointRounding.AwayFromZero);
                f.TravelNote = "Call-out R" + BaseCallOutFee.ToString("N2", CultureInfo.InvariantCulture) + " + round trip " + roundTrip.ToString("N1", CultureInfo.InvariantCulture)
                    + " km at R" + CostPerKilometer.ToString("N2", CultureInfo.InvariantCulture) + "/km.";
            }
            f.Subtotal = f.Services + f.Travel;
            f.Vat = Math.Round(f.Subtotal * VatRate, 2, MidpointRounding.AwayFromZero);
            f.Total = f.Subtotal + f.Vat;
            return f;
        }

        private static JObject FiguresJson(Figures f)
        {
            return new JObject {
                { "lines", JArray.FromObject(f.Lines) }, { "services", f.Services }, { "travel", f.Travel }, { "travelNote", f.TravelNote },
                { "oneWayKm", Math.Round(f.OneWayKm, 1) }, { "subtotal", f.Subtotal }, { "vat", f.Vat }, { "total", f.Total } };
        }

        private static bool HasBillableQuantity(string conn, int requestId)
        {
            using (var c = new SqlConnection(conn))
            using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM dbo.RequestItem ri INNER JOIN dbo.JobType jt ON ri.jobTypeID = jt.jobTypeID
                WHERE ri.jobRequestID = @id AND EXISTS (SELECT 1 FROM dbo.ItemDetail id WHERE id.requestItemID = ri.requestItemID
                  AND id.jobDetailID = " + BillableCase + " AND TRY_CONVERT(DECIMAL(18,2), id.detailValue) > 0)", c))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = requestId;
                c.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void QuotePreview(HttpContext ctx, string conn)
        {
            int requestId;
            if (!int.TryParse(ctx.Request.QueryString["requestId"], out requestId)) throw new V2Error(400, "requestId is required.");
            var req = Rows(conn, @"SELECT jr.jobRequestID, jr.status, jr.siteAddress, LTRIM(c.clientName + ' ' + ISNULL(c.clientSurname,'')) AS clientName
                FROM dbo.JobRequest jr INNER JOIN dbo.Client c ON c.clientID = jr.clientID WHERE jr.jobRequestID = @id", 1, P("@id", SqlDbType.Int, requestId));
            if (req.Count == 0) throw new V2Error(404, "Request not found.");
            string status = Convert.ToString(req[0]["status"]);
            var o = FiguresJson(Calculate(conn, requestId));
            o["requestId"] = requestId; o["clientName"] = Convert.ToString(req[0]["clientName"]); o["siteAddress"] = Convert.ToString(req[0]["siteAddress"]);
            o["requestStatus"] = status;
            bool exists = Scalar(conn, "SELECT COUNT(*) FROM dbo.Quote WHERE jobRequestID = @id", P("@id", SqlDbType.Int, requestId)) > 0;
            string blocker = null;
            if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) blocker = "This request has been cancelled, so a quote cannot be created.";
            else if (exists || status.Equals("Quote Sent", StringComparison.OrdinalIgnoreCase) || status.Equals("Quote Accepted", StringComparison.OrdinalIgnoreCase)) blocker = "A quote for this request already exists.";
            else if (!HasBillableQuantity(conn, requestId)) blocker = "Capture the measurements (quantities) for the requested services first.";
            o["canCreate"] = blocker == null; o["blocker"] = blocker;
            if (exists) o["quoteId"] = Convert.ToInt32(Scalar(conn, "SELECT TOP 1 QuoteID FROM dbo.Quote WHERE jobRequestID = @id ORDER BY QuoteID DESC", P("@id", SqlDbType.Int, requestId)));
            Json(ctx, o);
        }

        private const string QuoteSelect = @"SELECT q.QuoteID AS quoteId, q.jobRequestID AS requestId, LTRIM(c.clientName + ' ' + ISNULL(c.clientSurname,'')) AS clientName,
                c.phoneNumber AS clientPhone, c.emailAddress AS clientEmail, jr.siteAddress, q.dateIssued, q.expiryDate, q.amount, q.quoteStatus AS status
                FROM dbo.Quote q INNER JOIN dbo.JobRequest jr ON jr.jobRequestID = q.jobRequestID INNER JOIN dbo.Client c ON c.clientID = jr.clientID WHERE 1=1";

        private static void ListQuotes(HttpContext ctx, string conn)
        {
            string status = ctx.Request.QueryString["status"], search = (ctx.Request.QueryString["search"] ?? "").Trim();
            string sql = QuoteSelect; var ps = new List<SqlParameter>();
            if (!string.IsNullOrEmpty(status)) { sql += " AND q.quoteStatus = @st"; ps.Add(P("@st", SqlDbType.NVarChar, status)); }
            if (search.Length > 0)
            {
                sql += " AND (CONVERT(varchar(12), q.QuoteID) = @e OR CONVERT(varchar(12), q.jobRequestID) = @e OR c.clientName LIKE @s OR c.clientSurname LIKE @s OR jr.siteAddress LIKE @s)";
                ps.Add(P("@e", SqlDbType.VarChar, search)); ps.Add(P("@s", SqlDbType.VarChar, Like(search)));
            }
            sql += " ORDER BY q.dateIssued DESC, q.QuoteID DESC";
            Json(ctx, Rows(conn, sql, 300, ps.ToArray()));
        }

        private static JObject LoadQuote(string conn, int id)
        {
            var rows = Rows(conn, QuoteSelect + " AND q.QuoteID = @id", 1, P("@id", SqlDbType.Int, id));
            if (rows.Count == 0) throw new V2Error(404, "Quote not found.");
            var o = JObject.FromObject(rows[0]);
            var f = FiguresJson(Calculate(conn, Convert.ToInt32(rows[0]["requestId"])));
            o["breakdown"] = f;
            o["amountEdited"] = Math.Abs(Convert.ToDecimal(rows[0]["amount"]) - (decimal)f["total"]) > 0.009m;
            o["allowedStatuses"] = JArray.FromObject(QuoteStatuses);
            return o;
        }

        private static void GetQuote(HttpContext ctx, string conn, int id) { Json(ctx, LoadQuote(conn, id)); }

        private static void CreateQuote(HttpContext ctx, string conn)
        {
            int requestId = (int?)Body(ctx)["requestId"] ?? 0;
            var req = Rows(conn, "SELECT status FROM dbo.JobRequest WHERE jobRequestID = @id", 1, P("@id", SqlDbType.Int, requestId));
            if (req.Count == 0) throw new V2Error(404, "Request not found.");
            string status = Convert.ToString(req[0]["status"]) ?? "";
            if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)) throw new V2Error(409, "This request has been cancelled, so a quote cannot be created.");
            if (status.Equals("Quote Sent", StringComparison.OrdinalIgnoreCase) || status.Equals("Quote Accepted", StringComparison.OrdinalIgnoreCase))
                throw new V2Error(409, "A quote for this request already exists.");
            if (!HasBillableQuantity(conn, requestId)) throw new V2Error(409, "Cannot generate a quote yet. Valid billable quantities must be provided for the requested services.");

            Figures f = Calculate(conn, requestId);
            int quoteId;
            using (var c = new SqlConnection(conn))
            {
                c.Open();
                using (var tx = c.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand(@"INSERT INTO dbo.Quote (jobRequestID, dateIssued, expiryDate, dateGenerated, amount, quoteStatus)
                            SELECT @r, GETDATE(), DATEADD(month, 1, GETDATE()), GETDATE(), @a, 'Sent'
                            WHERE NOT EXISTS (SELECT 1 FROM dbo.Quote WHERE jobRequestID = @r);
                            SELECT ISNULL((SELECT TOP 1 QuoteID FROM dbo.Quote WHERE jobRequestID = @r ORDER BY QuoteID DESC), 0), @@ROWCOUNT;", c, tx))
                        {
                            cmd.Parameters.Add("@r", SqlDbType.Int).Value = requestId;
                            var a = cmd.Parameters.Add("@a", SqlDbType.Decimal); a.Precision = 18; a.Scale = 2; a.Value = f.Total;
                            using (var rd = cmd.ExecuteReader())
                            {
                                rd.Read();
                                quoteId = Convert.ToInt32(rd[0]);
                            }
                        }
                        using (var up = new SqlCommand("UPDATE dbo.JobRequest SET status = 'Quote Sent' WHERE jobRequestID = @r", c, tx))
                        {
                            up.Parameters.Add("@r", SqlDbType.Int).Value = requestId;
                            up.ExecuteNonQuery();
                        }
                        tx.Commit();
                    }
                    catch (Exception) { try { tx.Rollback(); } catch (Exception) { } throw; }
                }
            }
            ctx.Response.StatusCode = 201;
            Json(ctx, LoadQuote(conn, quoteId));
        }

        private static string MapJobStatus(string quoteStatus)
        {
            if (quoteStatus.Equals("Accepted", StringComparison.OrdinalIgnoreCase)) return "Quote Accepted";
            if (quoteStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase) || quoteStatus.Equals("Declined", StringComparison.OrdinalIgnoreCase)) return "Cancelled";
            if (quoteStatus.Equals("Sent", StringComparison.OrdinalIgnoreCase)) return "Quote Sent";
            return null;
        }

        private static void UpdateQuote(HttpContext ctx, string conn, int id)
        {
            JObject b = Body(ctx);
            var cur = Rows(conn, "SELECT quoteStatus, amount, expiryDate FROM dbo.Quote WHERE QuoteID = @id", 1, P("@id", SqlDbType.Int, id));
            if (cur.Count == 0) throw new V2Error(404, "Quote not found.");
            string oldStatus = (Convert.ToString(cur[0]["quoteStatus"]) ?? "").Trim();
            string newStatus = oldStatus;
            if (b["status"] != null && b["status"].Type != JTokenType.Null)
            {
                string s = ((string)b["status"] ?? "").Trim();
                newStatus = QuoteStatuses.FirstOrDefault(x => x.Equals(s, StringComparison.OrdinalIgnoreCase));
                if (newStatus == null) throw new V2Error(400, "Invalid quote status.");
            }
            decimal amount = Convert.ToDecimal(cur[0]["amount"]);
            if (b["amount"] != null && b["amount"].Type != JTokenType.Null)
            {
                if (!TryDecimal(b["amount"], out amount) || amount < 0 || amount > 100000000m) throw new V2Error(400, "Enter a valid amount.");
                amount = Math.Round(amount, 2);
            }
            DateTime? expiry = cur[0]["expiryDate"] == null ? (DateTime?)null : Convert.ToDateTime(cur[0]["expiryDate"]);
            if (b["expiryDate"] != null && b["expiryDate"].Type != JTokenType.Null)
            {
                DateTime d;
                if (!DateTime.TryParse((string)b["expiryDate"], CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) throw new V2Error(400, "Enter a valid expiry date.");
                expiry = d.Date;
            }
            using (var c = new SqlConnection(conn))
            {
                c.Open();
                using (var tx = c.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand("UPDATE dbo.Quote SET expiryDate = @e, amount = @a, quoteStatus = @s WHERE QuoteID = @id", c, tx))
                        {
                            cmd.Parameters.Add("@e", SqlDbType.DateTime).Value = (object)expiry ?? DBNull.Value;
                            var a = cmd.Parameters.Add("@a", SqlDbType.Decimal); a.Precision = 18; a.Scale = 2; a.Value = amount;
                            cmd.Parameters.Add("@s", SqlDbType.NVarChar, 50).Value = newStatus;
                            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                            cmd.ExecuteNonQuery();
                        }
                        string jobStatus = MapJobStatus(newStatus);
                        if (jobStatus != null && !string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
                        {
                            using (var up = new SqlCommand("UPDATE dbo.JobRequest SET status = @js WHERE jobRequestID = (SELECT jobRequestID FROM dbo.Quote WHERE QuoteID = @id)", c, tx))
                            {
                                up.Parameters.Add("@js", SqlDbType.NVarChar, 50).Value = jobStatus;
                                up.Parameters.Add("@id", SqlDbType.Int).Value = id;
                                up.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                    }
                    catch (Exception) { try { tx.Rollback(); } catch (Exception) { } throw; }
                }
            }
            Json(ctx, LoadQuote(conn, id));
        }

        // ================================================================== allocation
        private static List<int> SlotList(string csv)
        {
            var list = new List<int>();
            foreach (string s in (csv ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            { int v; if (int.TryParse(s.Trim(), out v)) list.Add(v); }
            return list.Distinct().ToList();
        }

        private static List<int> SlotList(JToken t)
        {
            var list = new List<int>();
            JArray a = t as JArray;
            if (a != null) foreach (var x in a) list.Add((int)x);
            return list.Distinct().ToList();
        }

        private static void StaffAllocations(HttpContext ctx, string conn)
        {
            string search = (ctx.Request.QueryString["search"] ?? "").Trim();
            string sql = @"SELECT jsa.staffID, jsa.jobID, jsa.assignmentDate, ISNULL(j.jobStatus,'Pending') AS jobStatus, jsa.hoursWorked,
                    ISNULL(s.firstName + ' ' + s.lastName, 'Staff #' + CAST(jsa.staffID AS VARCHAR)) AS staffName,
                    ISNULL(c.clientName + ' ' + c.clientSurname, 'N/A') AS clientName, ISNULL(jr.siteAddress, 'N/A') AS siteAddress,
                    STUFF((SELECT ', ' + CONVERT(varchar(5), ts.startTime, 108) + '-' + CONVERT(varchar(5), ts.endTime, 108)
                           FROM dbo.JobStaffAssignmentSlot sl INNER JOIN dbo.TimeSlot ts ON ts.timeSlotID = sl.timeSlotID
                           WHERE sl.staffID = jsa.staffID AND sl.jobID = jsa.jobID ORDER BY ts.startTime FOR XML PATH('')), 1, 2, '') AS slots
                FROM dbo.JobStaffAssignment jsa
                LEFT JOIN dbo.Staff s ON jsa.staffID = s.staffID LEFT JOIN dbo.Job j ON jsa.jobID = j.jobID
                LEFT JOIN dbo.Quote q ON j.quoteID = q.quoteID LEFT JOIN dbo.JobRequest jr ON q.jobRequestID = jr.jobRequestID LEFT JOIN dbo.Client c ON jr.clientID = c.clientID
                WHERE 1=1";
            var ps = new List<SqlParameter>();
            if (search.Length > 0)
            {
                sql += " AND (s.firstName LIKE @s OR s.lastName LIKE @s OR c.clientName LIKE @s OR c.clientSurname LIKE @s OR jr.siteAddress LIKE @s OR CONVERT(varchar(12), jsa.jobID) = @e OR j.jobStatus LIKE @s)";
                ps.Add(P("@s", SqlDbType.VarChar, Like(search))); ps.Add(P("@e", SqlDbType.VarChar, search));
            }
            sql += " ORDER BY jsa.assignmentDate DESC, jsa.jobID DESC";
            Json(ctx, Rows(conn, sql, 300, ps.ToArray()));
        }

        private static void AssetAllocations(HttpContext ctx, string conn)
        {
            string search = (ctx.Request.QueryString["search"] ?? "").Trim();
            string sql = @"SELECT a.AssignmentID AS assignmentId, a.jobID, a.assignmentDate,
                    COALESCE(o.type + ' (' + o.serialNumber + ')', h.equipmentType, 'Asset #' + CAST(ISNULL(a.ownedAssetID, a.hiredAssetID) AS VARCHAR)) AS assetName,
                    CASE WHEN a.ownedAssetID IS NOT NULL THEN 'Owned' WHEN a.hiredAssetID IS NOT NULL THEN 'Hired' ELSE 'Unspecified' END AS assetSource,
                    ISNULL(c.clientName + ' ' + c.clientSurname, 'N/A') AS clientName, ISNULL(jr.siteAddress, 'N/A') AS siteAddress,
                    STUFF((SELECT ', ' + CONVERT(varchar(5), ts.startTime, 108) + '-' + CONVERT(varchar(5), ts.endTime, 108)
                           FROM dbo.JobAssetAssignmentSlot sl INNER JOIN dbo.TimeSlot ts ON ts.timeSlotID = sl.timeSlotID
                           WHERE sl.assignmentID = a.AssignmentID ORDER BY ts.startTime FOR XML PATH('')), 1, 2, '') AS slots
                FROM dbo.JobAssetAssignment a
                LEFT JOIN dbo.OwnedAsset o ON a.ownedAssetID = o.assetID LEFT JOIN dbo.HiredAsset h ON a.hiredAssetID = h.hiredAssetID
                LEFT JOIN dbo.Job j ON a.jobID = j.jobID LEFT JOIN dbo.Quote q ON j.quoteID = q.quoteID
                LEFT JOIN dbo.JobRequest jr ON q.jobRequestID = jr.jobRequestID LEFT JOIN dbo.Client c ON jr.clientID = c.clientID
                WHERE 1=1";
            var ps = new List<SqlParameter>();
            if (search.Length > 0)
            {
                sql += " AND (o.type LIKE @s OR o.serialNumber LIKE @s OR h.equipmentType LIKE @s OR c.clientName LIKE @s OR c.clientSurname LIKE @s OR jr.siteAddress LIKE @s OR CONVERT(varchar(12), a.jobID) = @e)";
                ps.Add(P("@s", SqlDbType.VarChar, Like(search))); ps.Add(P("@e", SqlDbType.VarChar, search));
            }
            sql += " ORDER BY a.assignmentDate DESC, a.AssignmentID DESC";
            Json(ctx, Rows(conn, sql, 300, ps.ToArray()));
        }

        private static void JobAllocations(HttpContext ctx, string conn, int jobId)
        {
            var o = new JObject();
            o["jobId"] = jobId;
            o["staff"] = JArray.FromObject(Rows(conn, @"SELECT jsa.staffID, s.firstName + ' ' + s.lastName AS name, jsa.assignmentDate, jsa.hoursWorked,
                    STUFF((SELECT ', ' + CONVERT(varchar(5), ts.startTime, 108) + '-' + CONVERT(varchar(5), ts.endTime, 108)
                           FROM dbo.JobStaffAssignmentSlot sl INNER JOIN dbo.TimeSlot ts ON ts.timeSlotID = sl.timeSlotID
                           WHERE sl.staffID = jsa.staffID AND sl.jobID = jsa.jobID ORDER BY ts.startTime FOR XML PATH('')), 1, 2, '') AS slots
                FROM dbo.JobStaffAssignment jsa INNER JOIN dbo.Staff s ON s.staffID = jsa.staffID WHERE jsa.jobID = @id ORDER BY s.firstName", 100, P("@id", SqlDbType.Int, jobId)));
            o["assets"] = JArray.FromObject(Rows(conn, @"SELECT a.AssignmentID AS assignmentId, a.assignmentDate,
                    COALESCE(o.type + ' (' + o.serialNumber + ')', h.equipmentType, 'Asset') AS name,
                    CASE WHEN a.ownedAssetID IS NOT NULL THEN 'Owned' ELSE 'Hired' END AS source,
                    STUFF((SELECT ', ' + CONVERT(varchar(5), ts.startTime, 108) + '-' + CONVERT(varchar(5), ts.endTime, 108)
                           FROM dbo.JobAssetAssignmentSlot sl INNER JOIN dbo.TimeSlot ts ON ts.timeSlotID = sl.timeSlotID
                           WHERE sl.assignmentID = a.AssignmentID ORDER BY ts.startTime FOR XML PATH('')), 1, 2, '') AS slots
                FROM dbo.JobAssetAssignment a LEFT JOIN dbo.OwnedAsset o ON a.ownedAssetID = o.assetID LEFT JOIN dbo.HiredAsset h ON a.hiredAssetID = h.hiredAssetID
                WHERE a.jobID = @id ORDER BY a.assignmentDate", 100, P("@id", SqlDbType.Int, jobId)));
            Json(ctx, o);
        }

        /// <summary>Who/what can be booked for a date + time slots, with clash information (same rules as the website).</summary>
        private static void AssignOptions(HttpContext ctx, string conn, int jobId)
        {
            DateTime date = ParseDate(ctx.Request.QueryString["date"], DateTime.Today);
            List<int> slots = SlotList(ctx.Request.QueryString["slots"]);
            var staffBookings = slots.Count > 0 ? Scheduling.GetStaffBookings(date, jobId, slots) : new Dictionary<string, Scheduling.Booking>();
            var assetBookings = slots.Count > 0 ? Scheduling.GetAssetBookings(date, jobId, slots) : new Dictionary<string, Scheduling.Booking>();

            var staff = new JArray();
            foreach (var r in Rows(conn, @"SELECT s.staffID, s.firstName, s.lastName, s.contactNumber, ISNULL(r.roleName, 'General') AS roleName
                FROM dbo.Staff s LEFT JOIN dbo.Role r ON s.roleID = r.roleID
                WHERE s.staffStatus = 'Active' AND (r.accessLevel NOT IN (5, 6) OR r.accessLevel IS NULL) AND (s.roleID NOT IN (10, 11) OR s.roleID IS NULL)
                ORDER BY s.firstName, s.lastName", 300))
            {
                string key = Convert.ToString(r["staffID"]);
                Scheduling.Booking b; staffBookings.TryGetValue(key, out b);
                staff.Add(new JObject {
                    { "staffId", Convert.ToInt32(r["staffID"]) }, { "name", (Str(r["firstName"]) + " " + Str(r["lastName"])).Trim() },
                    { "role", Str(r["roleName"]) }, { "phone", Str(r["contactNumber"]) },
                    { "busy", b == null ? null : b.Describe() }, { "onThisJob", b != null && b.SameJob } });
            }

            var assets = new JArray();
            foreach (var r in Rows(conn, @"SELECT 'Owned' AS kind, assetID AS id, type AS name, ISNULL(serialNumber, 'N/A') AS identifier, 'Giant Group (Owned)' AS owner
                    FROM dbo.OwnedAsset WHERE assetStatus = 'Available'
                UNION ALL
                SELECT 'Hired', hiredAssetID, equipmentType, 'Hired Asset #' + CAST(hiredAssetID AS VARCHAR), ISNULL(supplierName, 'External Supplier')
                    FROM dbo.HiredAsset ORDER BY 1, 3", 400))
            {
                string kind = Str(r["kind"]);
                Scheduling.Booking b; assetBookings.TryGetValue(kind + ":" + Convert.ToString(r["id"]), out b);
                assets.Add(new JObject {
                    { "kind", kind }, { "assetId", Convert.ToInt32(r["id"]) }, { "name", Str(r["name"]) }, { "identifier", Str(r["identifier"]) },
                    { "owner", Str(r["owner"]) }, { "busy", b == null ? null : b.Describe() }, { "onThisJob", b != null && b.SameJob } });
            }
            Json(ctx, new JObject { { "staff", staff }, { "assets", assets } });
        }

        private static void RequireJob(string conn, int jobId)
        {
            if (Scalar(conn, "SELECT COUNT(*) FROM dbo.Job WHERE jobID = @id AND ISNULL(jobStatus,'') <> 'Archived'", P("@id", SqlDbType.Int, jobId)) == 0)
                throw new V2Error(404, "That job is no longer available.");
        }

        private static void AssignStaff(HttpContext ctx, string conn, int jobId)
        {
            JObject b = Body(ctx);
            RequireJob(conn, jobId);
            int staffId = (int?)b["staffId"] ?? 0;
            DateTime date = ParseDate((string)b["date"], DateTime.MinValue);
            if (staffId <= 0) throw new V2Error(400, "Choose a staff member.");
            if (date == DateTime.MinValue) throw new V2Error(400, "Choose a date.");
            string err = Scheduling.AssignStaff(staffId, jobId, date, SlotList(b["slotIds"]));
            if (err != null) throw new V2Error(409, err);
            JobAllocations(ctx, conn, jobId);
        }

        private static void AssignAsset(HttpContext ctx, string conn, int jobId)
        {
            JObject b = Body(ctx);
            RequireJob(conn, jobId);
            string kind = ((string)b["kind"] ?? "").Trim();
            int assetId = (int?)b["assetId"] ?? 0;
            DateTime date = ParseDate((string)b["date"], DateTime.MinValue);
            bool owned = kind.Equals("Owned", StringComparison.OrdinalIgnoreCase);
            if (!owned && !kind.Equals("Hired", StringComparison.OrdinalIgnoreCase)) throw new V2Error(400, "Choose an asset.");
            if (assetId <= 0) throw new V2Error(400, "Choose an asset.");
            if (date == DateTime.MinValue) throw new V2Error(400, "Choose a date.");
            string err = Scheduling.AssignAsset(owned, assetId, jobId, date, SlotList(b["slotIds"]));
            if (err != null) throw new V2Error(409, err);
            JobAllocations(ctx, conn, jobId);
        }

        private static void UnassignStaff(HttpContext ctx, string conn)
        {
            JObject b = Body(ctx);
            int staffId = (int?)b["staffId"] ?? 0, jobId = (int?)b["jobId"] ?? 0;
            using (var c = new SqlConnection(conn))
            {
                c.Open();
                using (var tx = c.BeginTransaction())
                {
                    try
                    {
                        Exec(c, tx, "DELETE FROM dbo.JobStaffAssignmentSlot WHERE staffID = @s AND jobID = @j", P("@s", SqlDbType.Int, staffId), P("@j", SqlDbType.Int, jobId));
                        int n = Exec(c, tx, "DELETE FROM dbo.JobStaffAssignment WHERE staffID = @s AND jobID = @j", P("@s", SqlDbType.Int, staffId), P("@j", SqlDbType.Int, jobId));
                        if (n == 0) throw new V2Error(404, "That assignment no longer exists.");
                        tx.Commit();
                    }
                    catch (Exception) { try { tx.Rollback(); } catch (Exception) { } throw; }
                }
            }
            Json(ctx, new JObject { { "ok", true } });
        }

        private static void UnassignAsset(HttpContext ctx, string conn)
        {
            int assignmentId = (int?)Body(ctx)["assignmentId"] ?? 0;
            using (var c = new SqlConnection(conn))
            {
                c.Open();
                using (var tx = c.BeginTransaction())
                {
                    try
                    {
                        Exec(c, tx, "DELETE FROM dbo.JobAssetAssignmentSlot WHERE assignmentID = @a", P("@a", SqlDbType.Int, assignmentId));
                        int n = Exec(c, tx, "DELETE FROM dbo.JobAssetAssignment WHERE AssignmentID = @a", P("@a", SqlDbType.Int, assignmentId));
                        if (n == 0) throw new V2Error(404, "That assignment no longer exists.");
                        tx.Commit();
                    }
                    catch (Exception) { try { tx.Rollback(); } catch (Exception) { } throw; }
                }
            }
            Json(ctx, new JObject { { "ok", true } });
        }

        // ================================================================== payments
        private static string PaymentDateColumn(string conn)
        {
            using (var c = new SqlConnection(conn))
            using (var cmd = new SqlCommand(@"SELECT TOP 1 COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'Payment' AND TABLE_SCHEMA = 'dbo' AND DATA_TYPE IN ('date','datetime','datetime2','smalldatetime') ORDER BY ORDINAL_POSITION", c))
            {
                c.Open();
                object o = cmd.ExecuteScalar();
                return o == null || o == DBNull.Value ? null : Convert.ToString(o);
            }
        }

        private static void ListPayments(HttpContext ctx, string conn)
        {
            string search = (ctx.Request.QueryString["search"] ?? "").Trim();
            string filter = (ctx.Request.QueryString["filter"] ?? "").Trim().ToLowerInvariant();   // outstanding | paid
            string sql = @"WITH T AS (SELECT jobID, SUM(amountPaid) AS paid FROM dbo.Payment GROUP BY jobID)
                SELECT j.jobID, jr.jobRequestID AS requestId, j.jobStatus AS status, LTRIM(c.clientName + ' ' + ISNULL(c.clientSurname,'')) AS clientName, jr.siteAddress,
                       q.amount AS invoiceTotal, CAST(ISNULL(T.paid, 0) AS decimal(18,2)) AS totalPaid, CAST(q.amount - ISNULL(T.paid, 0) AS decimal(18,2)) AS balance
                FROM dbo.Job j INNER JOIN dbo.Quote q ON q.QuoteID = j.quoteID INNER JOIN dbo.JobRequest jr ON jr.jobRequestID = q.jobRequestID
                INNER JOIN dbo.Client c ON c.clientID = jr.clientID LEFT JOIN T ON T.jobID = j.jobID
                WHERE ISNULL(j.jobStatus,'') <> 'Archived'";
            var ps = new List<SqlParameter>();
            if (filter == "outstanding") sql += " AND q.amount - ISNULL(T.paid, 0) > 0.009";
            else if (filter == "paid") sql += " AND q.amount - ISNULL(T.paid, 0) <= 0.009";
            if (search.Length > 0)
            {
                sql += " AND (CONVERT(varchar(12), j.jobID) = @e OR c.clientName LIKE @s OR c.clientSurname LIKE @s OR jr.siteAddress LIKE @s)";
                ps.Add(P("@e", SqlDbType.VarChar, search)); ps.Add(P("@s", SqlDbType.VarChar, Like(search)));
            }
            sql += " ORDER BY j.jobID DESC";
            Json(ctx, Rows(conn, sql, 300, ps.ToArray()));
        }

        private static JObject LoadJobPayments(string conn, int jobId)
        {
            var head = Rows(conn, @"SELECT j.jobID, jr.jobRequestID AS requestId, j.jobStatus AS status, LTRIM(c.clientName + ' ' + ISNULL(c.clientSurname,'')) AS clientName,
                    c.phoneNumber AS clientPhone, jr.siteAddress, q.amount AS invoiceTotal
                FROM dbo.Job j INNER JOIN dbo.Quote q ON q.QuoteID = j.quoteID INNER JOIN dbo.JobRequest jr ON jr.jobRequestID = q.jobRequestID
                INNER JOIN dbo.Client c ON c.clientID = jr.clientID WHERE j.jobID = @id", 1, P("@id", SqlDbType.Int, jobId));
            if (head.Count == 0) throw new V2Error(404, "Job not found.");
            var o = JObject.FromObject(head[0]);

            // Payment's date column name is not known at compile time, so discover it.
            string dateCol = PaymentDateColumn(conn);
            var hist = new JArray(); decimal paid = 0;
            foreach (var r in Rows(conn, "SELECT * FROM dbo.Payment WHERE jobID = @id", 200, P("@id", SqlDbType.Int, jobId)))
            {
                decimal amt = r.ContainsKey("amountPaid") && r["amountPaid"] != null ? Convert.ToDecimal(r["amountPaid"]) : 0m;
                paid += amt;
                DateTime? when = null;
                if (dateCol != null && r.ContainsKey(dateCol) && r[dateCol] != null) when = Convert.ToDateTime(r[dateCol]);
                hist.Add(new JObject {
                    { "amount", amt }, { "method", r.ContainsKey("paymentMethod") ? Str(r["paymentMethod"]) : null },
                    { "reference", r.ContainsKey("referenceNumber") ? Str(r["referenceNumber"]) : null }, { "date", when } });
            }
            var sorted = new JArray(hist.OrderByDescending(h => (DateTime?)h["date"] ?? DateTime.MinValue));
            decimal total = Convert.ToDecimal(head[0]["invoiceTotal"]);
            o["totalPaid"] = paid; o["balance"] = total - paid; o["payments"] = sorted;
            return o;
        }

        private static void JobPayments(HttpContext ctx, string conn, int jobId) { Json(ctx, LoadJobPayments(conn, jobId)); }

        private static void RecordPayment(HttpContext ctx, string conn, int jobId)
        {
            JObject b = Body(ctx);
            decimal amount;
            if (!TryDecimal(b["amount"], out amount)) throw new V2Error(400, "Enter the amount received.");
            amount = Math.Round(amount, 2);
            string method = ((string)b["method"] ?? "").Trim();
            string reference = ((string)b["reference"] ?? "").Trim();
            string[] methods = { "Cash", "EFT", "Card", "Other" };
            string canon = methods.FirstOrDefault(x => x.Equals(method, StringComparison.OrdinalIgnoreCase));
            if (canon == null) throw new V2Error(400, "Choose how the payment was made.");
            if (reference.Length > 100) throw new V2Error(400, "The reference is too long.");

            JObject cur = LoadJobPayments(conn, jobId);
            decimal balance = (decimal)cur["balance"];
            if (amount <= 0) throw new V2Error(400, "The amount must be more than zero.");
            if (amount > balance + 0.009m) throw new V2Error(409, "That is more than the outstanding balance of R " + balance.ToString("N2", CultureInfo.GetCultureInfo("en-ZA")) + ".");

            using (var c = new SqlConnection(conn))
            using (var cmd = new SqlCommand(@"INSERT INTO dbo.Payment (amountPaid, paymentMethod, jobID, referenceNumber) VALUES (@a, @m, @j, NULLIF(@r, ''))", c))
            {
                var a = cmd.Parameters.Add("@a", SqlDbType.Decimal); a.Precision = 10; a.Scale = 2; a.Value = amount;
                cmd.Parameters.Add("@m", SqlDbType.VarChar, 30).Value = canon;
                cmd.Parameters.Add("@j", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@r", SqlDbType.VarChar, 100).Value = reference;
                c.Open();
                cmd.ExecuteNonQuery();
            }
            ctx.Response.StatusCode = 201;
            Json(ctx, LoadJobPayments(conn, jobId));
        }

        // ================================================================== reports
        private static void Report(HttpContext ctx, string conn)
        {
            var o = new JObject();
            o["requestsByStatus"] = JArray.FromObject(Rows(conn, "SELECT ISNULL(status,'Pending') AS label, COUNT(*) AS count FROM dbo.JobRequest GROUP BY ISNULL(status,'Pending') ORDER BY COUNT(*) DESC", 20));
            o["jobsByStatus"] = JArray.FromObject(Rows(conn, "SELECT ISNULL(jobStatus,'Unknown') AS label, COUNT(*) AS count FROM dbo.Job WHERE ISNULL(jobStatus,'') <> 'Archived' GROUP BY ISNULL(jobStatus,'Unknown') ORDER BY COUNT(*) DESC", 20));
            o["quotesByStatus"] = JArray.FromObject(Rows(conn, "SELECT ISNULL(quoteStatus,'Pending') AS label, COUNT(*) AS count, ISNULL(SUM(amount),0) AS value FROM dbo.Quote GROUP BY ISNULL(quoteStatus,'Pending') ORDER BY COUNT(*) DESC", 20));
            decimal invoiced, paid; Totals(conn, out invoiced, out paid);
            o["invoiced"] = invoiced; o["collected"] = paid; o["outstanding"] = invoiced - paid;
            o["collectionRate"] = invoiced > 0 ? Math.Round(paid / invoiced * 100m, 1) : 0m;
            int sent = Convert.ToInt32(Scalar(conn, "SELECT COUNT(*) FROM dbo.Quote WHERE quoteStatus IN ('Sent','Accepted','Rejected','Declined')"));
            int accepted = Convert.ToInt32(Scalar(conn, "SELECT COUNT(*) FROM dbo.Quote WHERE quoteStatus = 'Accepted'"));
            o["quoteAcceptRate"] = sent > 0 ? Math.Round(accepted * 100m / sent, 1) : 0m;
            o["clients"] = Convert.ToInt32(Scalar(conn, "SELECT COUNT(*) FROM dbo.Client"));
            string dateCol = PaymentDateColumn(conn);
            if (dateCol != null)
            {
                string col = "[" + dateCol.Replace("]", "") + "]";
                o["collectedLast30Days"] = Scalar(conn, "SELECT ISNULL(SUM(amountPaid),0) FROM dbo.Payment WHERE " + col + " >= DATEADD(day,-30,GETDATE())");
                o["paymentsByMonth"] = JArray.FromObject(Rows(conn, "SELECT FORMAT(" + col + ", 'MMM yyyy') AS label, YEAR(" + col + ") * 100 + MONTH(" + col + ") AS sortKey, SUM(amountPaid) AS value FROM dbo.Payment WHERE " + col + " >= DATEADD(month,-5,DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),1)) GROUP BY FORMAT(" + col + ", 'MMM yyyy'), YEAR(" + col + ") * 100 + MONTH(" + col + ") ORDER BY 2", 12));
            }
            Json(ctx, o);
        }

        // ================================================================== helpers
        private static SqlParameter P(string n, SqlDbType t, object v) { return new SqlParameter(n, t) { Value = v ?? DBNull.Value }; }

        private static List<Dictionary<string, object>> Rows(string conn, string sql, int top, params SqlParameter[] ps)
        {
            var list = new List<Dictionary<string, object>>();
            using (var c = new SqlConnection(conn))
            using (var cmd = new SqlCommand(sql, c))
            {
                foreach (var p in ps) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.SqlDbType) { Value = p.Value });
                c.Open();
                using (var r = cmd.ExecuteReader())
                    while (list.Count < top && r.Read())
                    {
                        var d = new Dictionary<string, object>();
                        for (int i = 0; i < r.FieldCount; i++) d[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                        list.Add(d);
                    }
            }
            return list;
        }

        private static decimal Scalar(string conn, string sql, params SqlParameter[] ps)
        {
            using (var c = new SqlConnection(conn))
            using (var cmd = new SqlCommand(sql, c))
            {
                foreach (var p in ps) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.SqlDbType) { Value = p.Value });
                c.Open();
                object o = cmd.ExecuteScalar();
                return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
            }
        }

        private static int Exec(SqlConnection c, SqlTransaction tx, string sql, params SqlParameter[] ps)
        {
            using (var cmd = new SqlCommand(sql, c, tx))
            {
                foreach (var p in ps) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.SqlDbType) { Value = p.Value });
                return cmd.ExecuteNonQuery();
            }
        }

        private static string Like(string s) { return "%" + s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%"; }
        private static string Str(object o) { return o == null || o == DBNull.Value ? null : Convert.ToString(o); }
        private static JToken Dbl(object o) { return o == null || o == DBNull.Value ? (JToken)null : Convert.ToDouble(o); }

        private static DateTime ParseDate(string s, DateTime fallback)
        {
            DateTime d;
            return !string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ? d.Date : fallback;
        }

        private static bool TryDecimal(JToken t, out decimal v)
        {
            v = 0;
            if (t == null || t.Type == JTokenType.Null) return false;
            if (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) { v = Convert.ToDecimal((double)t, CultureInfo.InvariantCulture); return true; }
            return decimal.TryParse((string)t, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }

        private static JObject Body(HttpContext ctx)
        {
            try
            {
                using (var sr = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                {
                    string t = sr.ReadToEnd();
                    return t.Length == 0 ? new JObject() : JObject.Parse(t);
                }
            }
            catch (JsonException) { throw new V2Error(400, "Malformed JSON body."); }
        }

        private static void Json(HttpContext ctx, object o)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.Write(JsonConvert.SerializeObject(o, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                DateFormatString = "yyyy-MM-ddTHH:mm:ss"
            }));
        }

        private static void Fail(HttpContext ctx, int code, string msg)
        {
            ctx.Response.Clear();
            ctx.Response.StatusCode = code;
            ctx.Response.TrySkipIisCustomErrors = true;
            ctx.Response.ContentType = "application/json";
            ctx.Response.Write(JsonConvert.SerializeObject(new { error = msg }));
        }
    }
}
