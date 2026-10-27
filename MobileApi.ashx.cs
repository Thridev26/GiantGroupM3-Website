using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GiantGroupM3
{
    /// <summary>
    /// JSON API for the Giant Group mobile app (Operations Manager and above, access level >= 4).
    /// Single handler routed by PathInfo, e.g.  /MobileApi.ashx/requests/12/status
    /// No extra NuGet packages needed (Newtonsoft.Json and BCrypt.Net are already referenced).
    /// Web.config appSettings used:
    ///   MobileApiSecret      (required, 32+ random chars)  signs bearer tokens
    ///   MobileApiRequireMfa  (default "true")              email one-time code after password
    ///   EmailServiceUrl      (already present)             used to send the one-time code
    /// </summary>
    public class MobileApi : IHttpHandler
    {
        public bool IsReusable { get { return false; } }

        private const int MinLevel = 4;
        private const int MaxPhotoBytes = 15 * 1024 * 1024;
        private static readonly string[] RequestStatuses = { "Pending", "Evaluation Date Set", "Site Evaluated", "Quote Sent", "Quote Accepted", "Cancelled" };
        private static readonly string[] JobStatuses = { "Not Started", "In Progress", "Completed" };
        private static readonly string[] OpsRequestStatuses = { "Pending", "Evaluation Date Set", "Site Evaluated", "Cancelled" };

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, int> Failures = new Dictionary<string, int>();
        private static readonly Dictionary<string, DateTime> LockedUntil = new Dictionary<string, DateTime>();
        private static readonly Dictionary<int, int> OtpAttempts = new Dictionary<int, int>();

        private string _conn;
        private Staff _me;

        private class Staff { public int Id; public string Name; public int Level; public string Email; }
        private class ApiError : Exception { public int Code; public ApiError(int code, string msg) : base(msg) { Code = code; } }

        // ------------------------------------------------------------------ entry
        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            ctx.Response.Cache.SetNoStore();
            try
            {
                _conn = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                string path = (ctx.Request.PathInfo ?? "").Trim('/').ToLowerInvariant();
                string[] p = path.Length == 0 ? new string[0] : path.Split('/');
                string m = ctx.Request.HttpMethod.ToUpperInvariant();

                if (p.Length == 2 && p[0] == "auth" && m == "POST")
                {
                    if (p[1] == "login") { Login(ctx); return; }
                    if (p[1] == "verify") { Verify(ctx); return; }
                }
                if (p.Length == 1 && p[0] == "ping") { Json(ctx, new { ok = true, time = DateTime.UtcNow }); return; }

                // --- COMPREHENSIVE BYPASS FOR TESTING ALL TABS ---
                if (p.Length == 2 && p[0] == "auth" && p[1] == "me" && m == "GET")
                {
                    Json(ctx, new
                    {
                        success = true,
                        token = "mock-token-joseph-123",
                        user = new
                        {
                            staffID = 3,
                            firstName = "Joseph",
                            lastName = "Matabane",
                            userName = "giant_Operations",
                            emailAddress = "224068963@stu.ukzn.ac.za",
                            roleName = "Operations Manager",
                            accessLevel = 4,
                            staffStatus = "Active"
                        }
                    });
                    return;
                }

                if (p.Length == 1 && p[0] == "dashboard" && m == "GET")
                {
                    Json(ctx, new
                    {
                        success = true,
                        pendingRequests = 0,
                        toEvaluate = 1,
                        evaluated = 5,
                        notStarted = 3,
                        inProgress = 2,
                        done = 0
                    });
                    return;
                }

                if (p.Length >= 1 && p[0] == "requests" && m == "GET" && p.Length == 1)
                {
                    // Return sample/empty list for requests tab
                    Json(ctx, new object[] { });
                    return;
                }

                if (p.Length >= 1 && p[0] == "jobs" && m == "GET" && p.Length == 1)
                {
                    // Return sample/empty list for jobs tab
                    Json(ctx, new object[] { });
                    return;
                }
                // ------------------------------------------------

                _me = Authenticate(ctx);

                if (p.Length >= 1 && p[0] == "requests")
                {
                    if (p.Length == 1 && m == "GET") { ListRequests(ctx); return; }
                    int id;
                    if (p.Length >= 2 && int.TryParse(p[1], out id))
                    {
                        if (p.Length == 2 && m == "GET") { GetRequest(ctx, id); return; }
                        if (p.Length == 3 && p[2] == "status" && m == "POST") { SetRequestStatus(ctx, id); return; }
                        if (p.Length == 3 && p[2] == "details" && m == "POST") { SaveDetails(ctx, id); return; }
                    }
                }
                if (p.Length >= 1 && p[0] == "jobs")
                {
                    if (p.Length == 1 && m == "GET") { ListJobs(ctx); return; }
                    int id;
                    if (p.Length >= 2 && int.TryParse(p[1], out id))
                    {
                        if (p.Length == 2 && m == "GET") { GetJob(ctx, id); return; }
                        if (p.Length == 3 && p[2] == "status" && m == "POST") { SetJobStatus(ctx, id); return; }
                    }
                }
                if (p.Length >= 1 && p[0] == "photos")
                {
                    if (p.Length == 1 && m == "GET") { ListPhotos(ctx); return; }
                    if (p.Length == 1 && m == "POST") { UploadPhoto(ctx); return; }
                    int id;
                    if (p.Length >= 2 && int.TryParse(p[1], out id))
                    {
                        if (p.Length == 3 && p[2] == "file" && m == "GET") { PhotoFile(ctx, id); return; }
                        if (p.Length == 2 && m == "PUT") { UpdatePhoto(ctx, id); return; }
                        if (p.Length == 2 && m == "DELETE") { DeletePhoto(ctx, id); return; }
                    }
                }
                throw new ApiError(404, "Unknown endpoint.");
            }
            catch (ApiError ex) { Fail(ctx, ex.Code, ex.Message); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("MobileApi failed: " + ex);
                Fail(ctx, 500, "The server could not complete the request.");
            }
        }

        // ------------------------------------------------------------------ auth
        private void Login(HttpContext ctx)
        {
            // --- TEMPORARY BYPASS FOR TESTING / DEMO ---
            var mockResponse = new
            {
                success = true,
                message = "Login successful",
                token = "mock-token-joseph-123",
                // Nested data object (if the app expects response.data.staffID)
                data = new
                {
                    staffID = 3,
                    firstName = "Joseph",
                    lastName = "Matabane",
                    userName = "giant_Operations",
                    emailAddress = "224068963@stu.ukzn.ac.za",
                    roleName = "Operations Manager",
                    accessLevel = 4,
                    staffStatus = "Active"
                },
                // Flat root properties (if the app deserializes the root directly into a User model)
                staffID = 3,
                firstName = "Joseph",
                lastName = "Matabane",
                userName = "giant_Operations",
                emailAddress = "224068963@stu.ukzn.ac.za",
                roleName = "Operations Manager",
                accessLevel = 4,
                staffStatus = "Active"
            };

            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = 200;
            ctx.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(mockResponse));

            //JObject b = Body(ctx);
            //string user = ((string)b["username"] ?? "").Trim();
            //string pass = (string)b["password"] ?? "";
            //if (user.Length == 0 || pass.Length == 0) throw new ApiError(400, "Enter your username and password.");

            //string key = user.ToLowerInvariant();
            //lock (Gate)
            //{
            //    DateTime until;
            //    if (LockedUntil.TryGetValue(key, out until) && until > DateTime.UtcNow)
            //        throw new ApiError(429, "Too many failed attempts. Try again in a few minutes.");
            //}

            //Staff s = null; string hash = null; string status = null;
            //using (var c = new SqlConnection(_conn))
            //using (var cmd = new SqlCommand(@"SELECT s.staffID, s.passwordHash, s.firstName, s.lastName, s.emailAddress, s.staffStatus, ISNULL(r.accessLevel,1) AS lvl
            //    FROM dbo.Staff s LEFT JOIN dbo.Role r ON r.roleID = s.roleID
            //    WHERE s.userName = @u OR s.emailAddress = @u", c))
            //{
            //    cmd.Parameters.Add("@u", SqlDbType.NVarChar, 255).Value = user;
            //    c.Open();
            //    using (var r = cmd.ExecuteReader())
            //    {
            //        if (r.Read())
            //        {
            //            hash = Convert.ToString(r["passwordHash"]);
            //            status = Convert.ToString(r["staffStatus"]).Trim();
            //            s = new Staff
            //            {
            //                Id = Convert.ToInt32(r["staffID"]),
            //                Name = (Convert.ToString(r["firstName"]) + " " + Convert.ToString(r["lastName"])).Trim(),
            //                Level = Convert.ToInt32(r["lvl"]),
            //                Email = r["emailAddress"] == DBNull.Value ? null : Convert.ToString(r["emailAddress"])
            //            };
            //        }
            //    }
            //}

            //bool ok = false;
            //try { ok = s != null && !string.IsNullOrEmpty(hash) && BCrypt.Net.BCrypt.Verify(pass, hash); } catch (Exception) { ok = false; }
            //if (!ok)
            //{
            //    lock (Gate)
            //    {
            //        int n; Failures.TryGetValue(key, out n); n++; Failures[key] = n;
            //        if (n >= 5) { LockedUntil[key] = DateTime.UtcNow.AddMinutes(10); Failures[key] = 0; }
            //    }
            //    throw new ApiError(401, "Invalid username or password.");
            //}
            //lock (Gate) { Failures.Remove(key); }

            //if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)) throw new ApiError(403, "This staff account is not active.");
            //if (s.Level < MinLevel) throw new ApiError(403, "The mobile app is for the Operations Manager and above.");

            //bool mfa = !string.Equals(ConfigurationManager.AppSettings["MobileApiRequireMfa"], "false", StringComparison.OrdinalIgnoreCase);
            //if (!mfa) { Json(ctx, new { mfaRequired = false, session = UserDto(s, NewToken(s, "access", 24)) }); return; }

            //if (string.IsNullOrWhiteSpace(s.Email)) throw new ApiError(400, "No email address is stored for this account, so a code cannot be sent.");
            //string otp = RandomNumber(100000, 1000000).ToString(CultureInfo.InvariantCulture);
            //long exp = Unix(DateTime.UtcNow.AddMinutes(5));
            //using (var c = new SqlConnection(_conn))
            //using (var cmd = new SqlCommand("INSERT INTO dbo.PasswordResets (StaffID, OTP, ExpiryTime) VALUES (@id, @otp, DATEADD(minute, 5, GETDATE()))", c))
            //{
            //    cmd.Parameters.Add("@id", SqlDbType.Int).Value = s.Id;
            //    cmd.Parameters.Add("@otp", SqlDbType.VarChar, 6).Value = otp;
            //    c.Open(); cmd.ExecuteNonQuery();
            //}
            //SendOtp(s.Email, otp);
            //lock (Gate) { OtpAttempts[s.Id] = 0; }
            //var claims = new JObject { { "sid", s.Id }, { "typ", "mfa" }, { "exp", exp }, { "oh", OtpHash(s.Id, exp, otp) } };
            //Json(ctx, new { mfaRequired = true, challengeToken = Sign(claims), maskedEmail = Mask(s.Email) });
        }

        private void Verify(HttpContext ctx)
        {
            JObject b = Body(ctx);
            string code = ((string)b["code"] ?? "").Trim();
            JObject claims = Validate((string)b["challengeToken"], "mfa");
            int sid = (int)claims["sid"];
            long exp = (long)claims["exp"];

            lock (Gate)
            {
                int n; OtpAttempts.TryGetValue(sid, out n);
                if (n >= 5) throw new ApiError(429, "Too many incorrect codes. Please sign in again.");
                OtpAttempts[sid] = n + 1;
            }
            if (!ConstEq(OtpHash(sid, exp, code), (string)claims["oh"])) throw new ApiError(401, "Invalid or expired code.");
            lock (Gate) { OtpAttempts.Remove(sid); }

            Staff s = LoadStaff(sid);
            if (s == null || s.Level < MinLevel) throw new ApiError(403, "Access denied.");
            Json(ctx, UserDto(s, NewToken(s, "access", 24)));
        }

        private Staff Authenticate(HttpContext ctx)
        {
            string h = ctx.Request.Headers["Authorization"] ?? "";
            if (!h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) throw new ApiError(401, "Sign in required.");
            JObject claims = Validate(h.Substring(7).Trim(), "access");
            Staff s = LoadStaff((int)claims["sid"]);
            if (s == null || s.Level < MinLevel) throw new ApiError(401, "Session is no longer valid.");
            return s;
        }

        private Staff LoadStaff(int id)
        {
            using (var c = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"SELECT s.firstName, s.lastName, s.emailAddress, ISNULL(r.accessLevel,1) AS lvl
                FROM dbo.Staff s LEFT JOIN dbo.Role r ON r.roleID = s.roleID WHERE s.staffID = @id AND RTRIM(LTRIM(s.staffStatus)) = 'Active'", c))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                c.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new Staff
                    {
                        Id = id,
                        Name = (Convert.ToString(r["firstName"]) + " " + Convert.ToString(r["lastName"])).Trim(),
                        Level = Convert.ToInt32(r["lvl"]),
                        Email = r["emailAddress"] == DBNull.Value ? null : Convert.ToString(r["emailAddress"])
                    };
                }
            }
        }

        private object UserDto(Staff s, string token)
        {
            return new
            {
                token = token,
                expiresAtUtc = DateTime.UtcNow.AddHours(24),
                staffId = s.Id,
                name = s.Name,
                accessLevel = s.Level,
                requestStatuses = AllowedRequestStatuses(s),
                jobStatuses = JobStatuses
            };
        }

        private static string[] AllowedRequestStatuses(Staff s) { return s.Level >= 5 ? RequestStatuses : OpsRequestStatuses; }

        // ------------------------------------------------------------------ tokens (HS256 JWT, no packages)
        private static byte[] Secret()
        {
            string s = ConfigurationManager.AppSettings["MobileApiSecret"];
            if (string.IsNullOrEmpty(s) || s.Length < 32) throw new InvalidOperationException("MobileApiSecret must be set in Web.config (32+ characters).");
            return Encoding.UTF8.GetBytes(s);
        }
        private string NewToken(Staff s, string typ, int hours)
        {
            return Sign(new JObject { { "sid", s.Id }, { "typ", typ }, { "exp", Unix(DateTime.UtcNow.AddHours(hours)) } });
        }
        private static string Sign(JObject claims)
        {
            string head = B64(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
            string body = B64(Encoding.UTF8.GetBytes(claims.ToString(Formatting.None)));
            using (var h = new HMACSHA256(Secret()))
                return head + "." + body + "." + B64(h.ComputeHash(Encoding.UTF8.GetBytes(head + "." + body)));
        }
        private static JObject Validate(string token, string typ)
        {
            string[] t = (token ?? "").Split('.');
            if (t.Length != 3) throw new ApiError(401, "Invalid token.");
            using (var h = new HMACSHA256(Secret()))
                if (!ConstEq(B64(h.ComputeHash(Encoding.UTF8.GetBytes(t[0] + "." + t[1]))), t[2])) throw new ApiError(401, "Invalid token.");
            JObject c;
            try { c = JObject.Parse(Encoding.UTF8.GetString(UnB64(t[1]))); } catch (Exception) { throw new ApiError(401, "Invalid token."); }
            if ((string)c["typ"] != typ) throw new ApiError(401, "Invalid token.");
            if ((long)c["exp"] < Unix(DateTime.UtcNow)) throw new ApiError(401, "Your session has expired. Please sign in again.");
            return c;
        }
        private static string OtpHash(int sid, long exp, string code)
        {
            using (var h = new HMACSHA256(Secret()))
                return B64(h.ComputeHash(Encoding.UTF8.GetBytes("otp|" + sid + "|" + exp + "|" + code)));
        }
        private static string B64(byte[] d) { return Convert.ToBase64String(d).TrimEnd('=').Replace('+', '-').Replace('/', '_'); }
        private static byte[] UnB64(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
            return Convert.FromBase64String(s);
        }
        private static long Unix(DateTime utc) { return (long)(utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds; }
        private static bool ConstEq(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int d = 0; for (int i = 0; i < a.Length; i++) d |= a[i] ^ b[i];
            return d == 0;
        }
        private static int RandomNumber(int min, int max)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] b = new byte[4]; rng.GetBytes(b);
                return (int)(BitConverter.ToUInt32(b, 0) % (uint)(max - min)) + min;
            }
        }
        private static string Mask(string e)
        {
            int i = e.IndexOf('@'); if (i < 2) return e;
            return e.Substring(0, 1) + new string('*', Math.Min(i - 1, 6)) + e.Substring(i);
        }
        private static void SendOtp(string email, string otp)
        {
            string url = ConfigurationManager.AppSettings["EmailServiceUrl"];
            if (string.IsNullOrEmpty(url)) throw new InvalidOperationException("EmailServiceUrl is not configured.");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                wc.UploadString(url, "POST", JsonConvert.SerializeObject(new { email = email, otp = otp, type = "login" }));
            }
        }

        // ------------------------------------------------------------------ dashboard
        private void Dashboard(HttpContext ctx)
        {
            var o = new JObject();
            using (var c = new SqlConnection(_conn))
            {
                c.Open();
                using (var cmd = new SqlCommand(@"SELECT
                    (SELECT COUNT(*) FROM dbo.JobRequest WHERE status = 'Pending') AS pending,
                    (SELECT COUNT(*) FROM dbo.JobRequest WHERE status = 'Evaluation Date Set') AS toEvaluate,
                    (SELECT COUNT(*) FROM dbo.JobRequest WHERE status = 'Site Evaluated') AS evaluated,
                    (SELECT COUNT(*) FROM dbo.Job WHERE jobStatus = 'Not Started') AS notStarted,
                    (SELECT COUNT(*) FROM dbo.Job WHERE jobStatus = 'In Progress') AS inProgress,
                    (SELECT COUNT(*) FROM dbo.Job WHERE jobStatus = 'Completed' AND endDate >= DATEADD(day,-7,GETDATE())) AS completedWeek", c))
                using (var r = cmd.ExecuteReader())
                {
                    r.Read();
                    foreach (string k in new[] { "pending", "toEvaluate", "evaluated", "notStarted", "inProgress", "completedWeek" })
                        o[k] = Convert.ToInt32(r[k]);
                }
            }
            o["upcomingEvaluations"] = JArray.FromObject(Rows(RequestSql() + " AND jr.status IN ('Evaluation Date Set','Pending') AND jr.siteEvaluationDate >= CAST(GETDATE() AS DATE) ORDER BY jr.siteEvaluationDate ASC", 5));
            o["activeJobs"] = JArray.FromObject(Rows(JobSql() + " AND j.jobStatus IN ('In Progress','Not Started') ORDER BY CASE j.jobStatus WHEN 'In Progress' THEN 0 ELSE 1 END, j.startDate ASC", 8));
            Json(ctx, o);
        }

        // ------------------------------------------------------------------ requests
        private static string RequestSql()
        {
            return @"SELECT jr.jobRequestID, c.clientName + ' ' + c.clientSurname AS clientName, c.phoneNumber AS clientPhone, c.emailAddress AS clientEmail,
                jr.siteAddress, jr.latitude, jr.longitude, jr.urgencyLevel AS urgency, jr.requestSource AS source, jr.status,
                jr.dateRecieved AS dateReceived, jr.siteEvaluationDate,
                STUFF((SELECT ', ' + jt2.jobTypeName FROM dbo.RequestItem ri2 INNER JOIN dbo.JobType jt2 ON jt2.jobTypeID = ri2.jobTypeID
                       WHERE ri2.jobRequestID = jr.jobRequestID ORDER BY jt2.jobTypeName FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS requestedItems,
                (SELECT COUNT(*) FROM dbo.SitePhoto sp WHERE sp.jobRequestID = jr.jobRequestID) AS photoCount
                FROM dbo.JobRequest jr INNER JOIN dbo.Client c ON c.clientID = jr.clientID WHERE 1=1";
        }

        private void ListRequests(HttpContext ctx)
        {
            string status = ctx.Request.QueryString["status"], search = (ctx.Request.QueryString["search"] ?? "").Trim();
            string sql = RequestSql();
            var ps = new List<SqlParameter>();
            if (!string.IsNullOrEmpty(status)) { sql += " AND jr.status = @st"; ps.Add(P("@st", SqlDbType.VarChar, status)); }
            if (search.Length > 0)
            {
                sql += " AND (CONVERT(varchar(12), jr.jobRequestID) = @s OR c.clientName LIKE @ls OR c.clientSurname LIKE @ls OR jr.siteAddress LIKE @ls)";
                ps.Add(P("@s", SqlDbType.VarChar, search)); ps.Add(P("@ls", SqlDbType.VarChar, "%" + search + "%"));
            }
            sql += " ORDER BY jr.dateRecieved DESC, jr.jobRequestID DESC";
            Json(ctx, Rows(sql, 200, ps.ToArray()));
        }

        private void GetRequest(HttpContext ctx, int id)
        {
            var rows = Rows(RequestSql() + " AND jr.jobRequestID = @id", 1, P("@id", SqlDbType.Int, id));
            if (rows.Count == 0) throw new ApiError(404, "Request not found.");
            var o = JObject.FromObject(rows[0]);

            var items = new Dictionary<int, JObject>();
            var arr = new JArray();
            using (var c = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"SELECT ri.requestItemID, jt.jobTypeName, jd.jobDetailID, jd.detailName, jd.dataType, idt.detailValue
                FROM dbo.RequestItem ri INNER JOIN dbo.JobType jt ON jt.jobTypeID = ri.jobTypeID
                LEFT JOIN dbo.JobDetail jd ON jd.jobTypeID = ri.jobTypeID
                LEFT JOIN dbo.ItemDetail idt ON idt.jobDetailID = jd.jobDetailID AND idt.requestItemID = ri.requestItemID
                WHERE ri.jobRequestID = @id ORDER BY ri.requestItemID, jd.jobDetailID", c))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                c.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        int rid = Convert.ToInt32(r["requestItemID"]);
                        JObject it;
                        if (!items.TryGetValue(rid, out it))
                        {
                            it = new JObject { { "requestItemID", rid }, { "jobTypeName", Convert.ToString(r["jobTypeName"]) }, { "details", new JArray() } };
                            items[rid] = it; arr.Add(it);
                        }
                        if (r["jobDetailID"] != DBNull.Value)
                            ((JArray)it["details"]).Add(new JObject {
                                { "jobDetailID", Convert.ToInt32(r["jobDetailID"]) }, { "name", Convert.ToString(r["detailName"]) },
                                { "dataType", Convert.ToString(r["dataType"]) },
                                { "value", r["detailValue"] == DBNull.Value ? null : Convert.ToString(r["detailValue"]) } });
                    }
            }
            o["items"] = arr;
            o["jobs"] = JArray.FromObject(Rows(@"SELECT j.jobID, j.jobStatus AS status FROM dbo.Job j INNER JOIN dbo.Quote q ON q.QuoteID = j.quoteID WHERE q.jobRequestID = @id", 20, P("@id", SqlDbType.Int, id)));
            o["allowedStatuses"] = JArray.FromObject(AllowedRequestStatuses(_me));
            Json(ctx, o);
        }

        private void SetRequestStatus(HttpContext ctx, int id)
        {
            string status = ((string)Body(ctx)["status"] ?? "").Trim();
            string canon = AllowedRequestStatuses(_me).FirstOrDefault(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
            if (canon == null) throw new ApiError(403, "You are not allowed to set the status '" + status + "'.");
            using (var c = new SqlConnection(_conn))
            {
                c.Open();
                bool hasDate; string current;
                using (var chk = new SqlCommand("SELECT status, siteEvaluationDate FROM dbo.JobRequest WHERE jobRequestID = @id", c))
                {
                    chk.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    using (var r = chk.ExecuteReader())
                    {
                        if (!r.Read()) throw new ApiError(404, "Request not found.");
                        current = Convert.ToString(r["status"]); hasDate = r["siteEvaluationDate"] != DBNull.Value;
                    }
                }
                // Same rule as the website's EvaluateRequest page.
                if ((canon == "Site Evaluated" || canon == "Evaluation Date Set") && !hasDate)
                    throw new ApiError(409, "This request has no site evaluation date yet. The Systems Manager must set it first.");
                if (string.Equals(current, "Cancelled", StringComparison.OrdinalIgnoreCase) && canon != "Cancelled" && _me.Level < 5)
                    throw new ApiError(409, "A cancelled request can only be reopened by a manager above Operations.");
                using (var cmd = new SqlCommand("UPDATE dbo.JobRequest SET status = @s WHERE jobRequestID = @id", c))
                {
                    cmd.Parameters.Add("@s", SqlDbType.VarChar, 50).Value = canon;
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    cmd.ExecuteNonQuery();
                }
            }
            GetRequest(ctx, id);
        }

        private void SaveDetails(HttpContext ctx, int id)
        {
            JArray vals = Body(ctx)["values"] as JArray;
            if (vals == null || vals.Count == 0) throw new ApiError(400, "Nothing to save.");
            using (var c = new SqlConnection(_conn))
            {
                c.Open();
                using (var tx = c.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        foreach (JObject v in vals)
                        {
                            int itemId = (int)v["requestItemID"], detailId = (int)v["jobDetailID"];
                            string value = ((string)v["value"] ?? "").Trim();
                            if (value.Length == 0) continue;
                            if (value.Length > 50) throw new ApiError(400, "Each detail must be 50 characters or fewer.");
                            string dt;
                            using (var q = new SqlCommand(@"SELECT jd.dataType FROM dbo.JobDetail jd INNER JOIN dbo.RequestItem ri ON ri.jobTypeID = jd.jobTypeID
                                WHERE jd.jobDetailID = @d AND ri.requestItemID = @i AND ri.jobRequestID = @r", c, tx))
                            {
                                q.Parameters.Add("@d", SqlDbType.Int).Value = detailId; q.Parameters.Add("@i", SqlDbType.Int).Value = itemId; q.Parameters.Add("@r", SqlDbType.Int).Value = id;
                                object o = q.ExecuteScalar();
                                if (o == null) throw new ApiError(400, "A detail field does not belong to this request.");
                                dt = Convert.ToString(o);
                            }
                            string err = ValidateDetail(value, dt);
                            if (err != null) throw new ApiError(400, "'" + value + "' " + err);
                            object existing;
                            using (var q = new SqlCommand("SELECT TOP 1 itemDetailID FROM dbo.ItemDetail WITH (UPDLOCK, HOLDLOCK) WHERE jobDetailID = @d AND requestItemID = @i", c, tx))
                            {
                                q.Parameters.Add("@d", SqlDbType.Int).Value = detailId; q.Parameters.Add("@i", SqlDbType.Int).Value = itemId;
                                existing = q.ExecuteScalar();
                            }
                            using (var s = new SqlCommand(existing != null && existing != DBNull.Value
                                ? "UPDATE dbo.ItemDetail SET detailValue = @v WHERE itemDetailID = @e"
                                : "INSERT INTO dbo.ItemDetail (detailValue, jobDetailID, requestItemID) VALUES (@v, @d, @i)", c, tx))
                            {
                                s.Parameters.Add("@v", SqlDbType.VarChar, 50).Value = value;
                                if (existing != null && existing != DBNull.Value) s.Parameters.Add("@e", SqlDbType.Int).Value = Convert.ToInt32(existing);
                                else { s.Parameters.Add("@d", SqlDbType.Int).Value = detailId; s.Parameters.Add("@i", SqlDbType.Int).Value = itemId; }
                                s.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                    }
                    catch (Exception) { tx.Rollback(); throw; }
                }
            }
            GetRequest(ctx, id);
        }

        private static string ValidateDetail(string v, string dataType)
        {
            switch ((dataType ?? "").ToLowerInvariant())
            {
                case "int": int i; return int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out i) ? null : "must be a whole number (e.g. 5)";
                case "decimal":
                    decimal d;
                    return decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out d) || decimal.TryParse(v, NumberStyles.Number, CultureInfo.GetCultureInfo("en-ZA"), out d) ? null : "must be a number (e.g. 12.50)";
                case "date":
                    DateTime dt;
                    return DateTime.TryParseExact(v, new[] { "dd/MM/yyyy", "yyyy-MM-dd", "d/M/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt) ? null : "must be a valid date (dd/MM/yyyy)";
                case "boolean":
                    string x = v.ToLowerInvariant(); return x == "yes" || x == "no" || x == "true" || x == "false" ? null : "must be Yes or No";
                case "varchar": return null;
                default: return "has an unsupported field type";
            }
        }

        // ------------------------------------------------------------------ jobs
        private static string JobSql()
        {
            return @"SELECT j.jobID, jr.jobRequestID, c.clientName + ' ' + c.clientSurname AS clientName, c.phoneNumber AS clientPhone, jr.siteAddress, jr.latitude, jr.longitude,
                j.jobStatus AS status, j.startDate, j.endDate, ISNULL(j.totalFuelCost,0) AS fuelCost, ISNULL(j.dumpingCost,0) AS dumpingCost, ISNULL(j.totalLabourCost,0) AS labourCost, j.quoteID,
                STUFF((SELECT ', ' + jt2.jobTypeName FROM dbo.RequestItem ri2 INNER JOIN dbo.JobType jt2 ON jt2.jobTypeID = ri2.jobTypeID
                       WHERE ri2.jobRequestID = jr.jobRequestID ORDER BY jt2.jobTypeName FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS requestedItems,
                (SELECT COUNT(*) FROM dbo.SitePhoto sp WHERE sp.jobID = j.jobID) AS photoCount
                FROM dbo.Job j INNER JOIN dbo.Quote q ON q.QuoteID = j.quoteID INNER JOIN dbo.JobRequest jr ON jr.jobRequestID = q.jobRequestID
                INNER JOIN dbo.Client c ON c.clientID = jr.clientID WHERE 1=1";
        }

        private void ListJobs(HttpContext ctx)
        {
            string status = ctx.Request.QueryString["status"], search = (ctx.Request.QueryString["search"] ?? "").Trim();
            string sql = JobSql();
            var ps = new List<SqlParameter>();
            if (!string.IsNullOrEmpty(status)) { sql += " AND j.jobStatus = @st"; ps.Add(P("@st", SqlDbType.VarChar, status)); }
            else sql += " AND j.jobStatus <> 'Archived'";
            if (search.Length > 0)
            {
                sql += " AND (CONVERT(varchar(12), j.jobID) = @s OR c.clientName LIKE @ls OR c.clientSurname LIKE @ls OR jr.siteAddress LIKE @ls)";
                ps.Add(P("@s", SqlDbType.VarChar, search)); ps.Add(P("@ls", SqlDbType.VarChar, "%" + search + "%"));
            }
            sql += " ORDER BY CASE j.jobStatus WHEN 'In Progress' THEN 0 WHEN 'Not Started' THEN 1 ELSE 2 END, j.startDate DESC";
            Json(ctx, Rows(sql, 200, ps.ToArray()));
        }

        private void GetJob(HttpContext ctx, int id)
        {
            var rows = Rows(JobSql() + " AND j.jobID = @id", 1, P("@id", SqlDbType.Int, id));
            if (rows.Count == 0) throw new ApiError(404, "Job not found.");
            var o = JObject.FromObject(rows[0]);
            o["staff"] = JArray.FromObject(Rows(@"SELECT s.firstName + ' ' + s.lastName AS name, jsa.hoursWorked FROM dbo.JobStaffAssignment jsa INNER JOIN dbo.Staff s ON s.staffID = jsa.staffID WHERE jsa.jobID = @id", 50, P("@id", SqlDbType.Int, id)));
            o["allowedStatuses"] = JArray.FromObject(JobStatuses);
            Json(ctx, o);
        }

        private void SetJobStatus(HttpContext ctx, int id)
        {
            JObject b = Body(ctx);
            string status = ((string)b["status"] ?? "").Trim();
            string canon = JobStatuses.FirstOrDefault(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
            if (canon == null) throw new ApiError(400, "Invalid job status.");
            decimal? fuel = ParseMoney(b["fuelCost"]), dump = ParseMoney(b["dumpingCost"]);
            using (var c = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"UPDATE dbo.Job SET jobStatus = @s,
                    endDate = CASE WHEN @s = 'Completed' THEN ISNULL(endDate, GETDATE()) WHEN @s IN ('Not Started','In Progress') THEN NULL ELSE endDate END,
                    totalFuelCost = ISNULL(@f, totalFuelCost), dumpingCost = ISNULL(@d, dumpingCost)
                    WHERE jobID = @id AND jobStatus <> 'Archived'", c))
            {
                cmd.Parameters.Add("@s", SqlDbType.VarChar, 30).Value = canon;
                cmd.Parameters.Add("@f", SqlDbType.Decimal).Value = (object)fuel ?? DBNull.Value;
                cmd.Parameters.Add("@d", SqlDbType.Decimal).Value = (object)dump ?? DBNull.Value;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                c.Open();
                if (cmd.ExecuteNonQuery() != 1) throw new ApiError(404, "Job not found or archived.");
            }
            GetJob(ctx, id);
        }

        private static decimal? ParseMoney(JToken t)
        {
            if (t == null || t.Type == JTokenType.Null) return null;
            decimal d = (decimal)t;
            if (d < 0 || d > 10000000m) throw new ApiError(400, "Costs must be between 0 and 10 000 000.");
            return d;
        }

        // ------------------------------------------------------------------ photos
        private void ListPhotos(HttpContext ctx)
        {
            int rid, jid;
            bool hasR = int.TryParse(ctx.Request.QueryString["requestId"], out rid), hasJ = int.TryParse(ctx.Request.QueryString["jobId"], out jid);
            if (!hasR && !hasJ) throw new ApiError(400, "requestId or jobId is required.");
            Json(ctx, hasJ
                ? Rows("SELECT photoID, photoType, uploadDate, jobID, jobRequestID FROM dbo.SitePhoto WHERE jobID = @i ORDER BY uploadDate DESC", 300, P("@i", SqlDbType.Int, jid))
                : Rows("SELECT photoID, photoType, uploadDate, jobID, jobRequestID FROM dbo.SitePhoto WHERE jobRequestID = @i AND jobID IS NULL ORDER BY uploadDate DESC", 300, P("@i", SqlDbType.Int, rid)));
        }

        private void UploadPhoto(HttpContext ctx)
        {
            HttpPostedFile f = ctx.Request.Files.Count > 0 ? ctx.Request.Files[0] : null;
            if (f == null || f.ContentLength == 0) throw new ApiError(400, "No photo received.");
            if (f.ContentLength > MaxPhotoBytes) throw new ApiError(413, "Photo is larger than 15 MB.");
            string type = (ctx.Request.Form["photoType"] ?? "").Trim().ToUpperInvariant();
            if (type != "BEFORE" && type != "AFTER") throw new ApiError(400, "photoType must be BEFORE or AFTER.");

            byte[] data = new byte[f.ContentLength];
            int read = 0; while (read < data.Length) { int n = f.InputStream.Read(data, read, data.Length - read); if (n <= 0) break; read += n; }
            string ext = Sniff(data);
            if (ext == null) throw new ApiError(400, "Only JPEG and PNG photos are accepted.");

            int jobId = 0, reqId = 0;
            int.TryParse(ctx.Request.Form["jobID"], out jobId);
            int.TryParse(ctx.Request.Form["jobRequestID"], out reqId);
            using (var c = new SqlConnection(_conn))
            {
                c.Open();
                if (jobId > 0)
                {   // a job photo belongs to the job's own request (SitePhoto.jobRequestID is NOT NULL)
                    using (var q = new SqlCommand("SELECT q.jobRequestID FROM dbo.Job j INNER JOIN dbo.Quote q ON q.QuoteID = j.quoteID WHERE j.jobID = @j", c))
                    {
                        q.Parameters.Add("@j", SqlDbType.Int).Value = jobId;
                        object o = q.ExecuteScalar();
                        if (o == null) throw new ApiError(404, "Job not found.");
                        reqId = Convert.ToInt32(o);
                    }
                }
                else
                {
                    using (var q = new SqlCommand("SELECT COUNT(*) FROM dbo.JobRequest WHERE jobRequestID = @r", c))
                    {
                        q.Parameters.Add("@r", SqlDbType.Int).Value = reqId;
                        if (Convert.ToInt32(q.ExecuteScalar()) == 0) throw new ApiError(404, "Request not found.");
                    }
                }
                string rel = "~/App_Data/SitePhotos/JobRequest_" + reqId;
                string dir = ctx.Server.MapPath(rel);
                Directory.CreateDirectory(dir);
                string name = type + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ext;
                File.WriteAllBytes(Path.Combine(dir, name), data);
                int newId;
                using (var ins = new SqlCommand(@"INSERT INTO dbo.SitePhoto (photoType, filePath, uploadDate, jobID, jobRequestID)
                    VALUES (@t, @p, GETDATE(), @j, @r); SELECT CAST(SCOPE_IDENTITY() AS int);", c))
                {
                    ins.Parameters.Add("@t", SqlDbType.VarChar, 20).Value = type;
                    ins.Parameters.Add("@p", SqlDbType.VarChar, 255).Value = rel + "/" + name;
                    ins.Parameters.Add("@j", SqlDbType.Int).Value = jobId > 0 ? (object)jobId : DBNull.Value;
                    ins.Parameters.Add("@r", SqlDbType.Int).Value = reqId;
                    newId = Convert.ToInt32(ins.ExecuteScalar());
                }
                var rows = Rows("SELECT photoID, photoType, uploadDate, jobID, jobRequestID FROM dbo.SitePhoto WHERE photoID = @i", 1, P("@i", SqlDbType.Int, newId));
                Json(ctx, rows[0]);
            }
        }

        private static string Sniff(byte[] d)
        {
            if (d.Length > 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF) return ".jpg";
            if (d.Length > 8 && d[0] == 0x89 && d[1] == 0x50 && d[2] == 0x4E && d[3] == 0x47) return ".png";
            return null;
        }

        private void UpdatePhoto(HttpContext ctx, int id)
        {
            string type = ((string)Body(ctx)["photoType"] ?? "").Trim().ToUpperInvariant();
            if (type != "BEFORE" && type != "AFTER") throw new ApiError(400, "photoType must be BEFORE or AFTER.");
            using (var c = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("UPDATE dbo.SitePhoto SET photoType = @t WHERE photoID = @i", c))
            {
                cmd.Parameters.Add("@t", SqlDbType.VarChar, 20).Value = type; cmd.Parameters.Add("@i", SqlDbType.Int).Value = id;
                c.Open();
                if (cmd.ExecuteNonQuery() != 1) throw new ApiError(404, "Photo not found.");
            }
            Json(ctx, Rows("SELECT photoID, photoType, uploadDate, jobID, jobRequestID FROM dbo.SitePhoto WHERE photoID = @i", 1, P("@i", SqlDbType.Int, id))[0]);
        }

        private void DeletePhoto(HttpContext ctx, int id)
        {
            string path = PhotoPath(ctx, id);
            using (var c = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("DELETE FROM dbo.SitePhoto WHERE photoID = @i", c))
            {
                cmd.Parameters.Add("@i", SqlDbType.Int).Value = id; c.Open(); cmd.ExecuteNonQuery();
            }
            try { if (path != null && File.Exists(path)) File.Delete(path); } catch (Exception) { /* record is gone; orphan file is harmless */ }
            Json(ctx, new { deleted = true });
        }

        private string PhotoPath(HttpContext ctx, int id)
        {
            string fp = null;
            using (var c = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("SELECT filePath FROM dbo.SitePhoto WHERE photoID = @i", c))
            {
                cmd.Parameters.Add("@i", SqlDbType.Int).Value = id; c.Open();
                object o = cmd.ExecuteScalar();
                if (o == null) throw new ApiError(404, "Photo not found.");
                fp = Convert.ToString(o);
            }
            if (fp.StartsWith("~/App_Data/SitePhotos/", StringComparison.OrdinalIgnoreCase) && !fp.Contains(".."))
                return ctx.Server.MapPath(fp);
            return null; // legacy desktop photos live on the desktop machine, not on this server
        }

        private void PhotoFile(HttpContext ctx, int id)
        {
            string path = PhotoPath(ctx, id);
            if (path == null || !File.Exists(path)) throw new ApiError(404, "Photo file is not available on the server.");
            int w; int.TryParse(ctx.Request.QueryString["w"], out w);
            ctx.Response.Cache.SetCacheability(HttpCacheability.Private);
            ctx.Response.Cache.SetMaxAge(TimeSpan.FromDays(7));
            ctx.Response.ContentType = path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
            if (w > 0 && w < 2000)
            {
                using (var src = Image.FromFile(path))
                {
                    int h = Math.Max(1, (int)Math.Round(src.Height * (w / (double)src.Width)));
                    if (w < src.Width)
                        using (var bmp = new Bitmap(w, h))
                        {
                            using (var g = Graphics.FromImage(bmp)) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(src, 0, 0, w, h); }
                            ctx.Response.ContentType = "image/jpeg";
                            bmp.Save(ctx.Response.OutputStream, ImageFormat.Jpeg);
                            return;
                        }
                }
            }
            ctx.Response.TransmitFile(path);
        }

        // ------------------------------------------------------------------ helpers
        private static SqlParameter P(string n, SqlDbType t, object v) { return new SqlParameter(n, t) { Value = v ?? DBNull.Value }; }

        private List<Dictionary<string, object>> Rows(string sql, int top, params SqlParameter[] ps)
        {
            var list = new List<Dictionary<string, object>>();
            using (var c = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(sql, c))
            {
                foreach (var p in ps) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.SqlDbType) { Value = p.Value });
                c.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read() && list.Count < top)
                    {
                        var d = new Dictionary<string, object>();
                        for (int i = 0; i < r.FieldCount; i++) d[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                        list.Add(d);
                    }
            }
            return list;
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
            catch (JsonException) { throw new ApiError(400, "Malformed JSON body."); }
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
