<%@ WebHandler Language="C#" Class="GiantGroupM3.CalendarEvents" %>

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;

namespace GiantGroupM3
{
    /// <summary>
    /// Personal calendar API used by Calendar.aspx.
    ///   ?action=events&amp;start=&amp;end=&amp;scope=me|team&amp;staffId=  FullCalendar event feed
    ///   ?action=stats&amp;scope=me|team&amp;staffId=                    dashboard tiles + "next up"
    ///   ?action=staff                                            people list for the team picker
    ///   ?action=ics                                              .ics file of the signed-in worker's schedule
    /// Crew (access level below 4) only ever see their own jobs.
    /// Operations Manager and above (level 4+) also see site evaluations and may switch to the whole team.
    /// </summary>
    public class CalendarEvents : IHttpHandler, IReadOnlySessionState
    {
        private const int TeamAccessLevel = 4;
        private const int PastDays = 7;
        private const int FutureDays = 120;
        private const int MaxRangeDays = 400;

        public bool IsReusable { get { return false; } }

        private sealed class Me
        {
            public int StaffId;
            public string FirstName = "";
            public string FullName = "";
            public int Level;
            public bool CanSeeTeam { get { return Level >= TeamAccessLevel; } }
        }

        private sealed class Scope
        {
            public bool Team;
            public int FilterStaffId;   // 0 = everyone (team view only)
        }

        private sealed class CalEvent
        {
            public string Kind = "";    // "job" or "evaluation"
            public int Id;              // jobID, or jobRequestID for evaluations
            public DateTime Date;
            public string Status = "";
            public string Client = "";
            public string Phone = "";
            public string Address = "";
            public string Items = "";
            public string Crew = "";
            public string Urgency = "";
            public int RequestId;
            public double? Lat;
            public double? Lng;
            public double TotalHours;
            public double MyHours;
            public bool IsMine;
        }

        // Jobs that start within the window. Crew filter: @crew = 0 means everyone.
        private const string JobSql = @"
            SELECT j.jobID, j.jobStatus, j.startDate,
                   jr.jobRequestID, jr.siteAddress, jr.latitude, jr.longitude, jr.urgencyLevel,
                   LTRIM(ISNULL(c.clientName, '') + ' ' + ISNULL(c.clientSurname, '')) AS clientName,
                   c.phoneNumber AS clientPhone,
                   STUFF((SELECT ', ' + jt.jobTypeName
                          FROM dbo.RequestItem ri
                          INNER JOIN dbo.JobType jt ON jt.jobTypeID = ri.jobTypeID
                          WHERE ri.jobRequestID = jr.jobRequestID
                          ORDER BY jt.jobTypeName
                          FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS items,
                   STUFF((SELECT ', ' + LTRIM(ISNULL(st.firstName, '') + ' ' + ISNULL(st.lastName, ''))
                          FROM dbo.JobStaffAssignment ja2
                          INNER JOIN dbo.Staff st ON st.staffID = ja2.staffID
                          WHERE ja2.jobID = j.jobID
                          ORDER BY st.firstName
                          FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS crew,
                   (SELECT ISNULL(SUM(a.hoursWorked), 0) FROM dbo.JobStaffAssignment a WHERE a.jobID = j.jobID) AS totalHours,
                   (SELECT ISNULL(SUM(a.hoursWorked), 0) FROM dbo.JobStaffAssignment a WHERE a.jobID = j.jobID AND a.staffID = @me) AS myHours,
                   CASE WHEN EXISTS (SELECT 1 FROM dbo.JobStaffAssignment a WHERE a.jobID = j.jobID AND a.staffID = @me) THEN 1 ELSE 0 END AS isMine
            FROM dbo.Job j
            LEFT JOIN dbo.Quote q ON q.QuoteID = j.quoteID
            LEFT JOIN dbo.JobRequest jr ON jr.jobRequestID = q.jobRequestID
            LEFT JOIN dbo.Client c ON c.clientID = jr.clientID
            WHERE j.startDate IS NOT NULL
              AND j.startDate >= @from AND j.startDate < @to
              AND ISNULL(j.jobStatus, '') <> 'Archived'
              AND (@crew = 0 OR EXISTS (SELECT 1 FROM dbo.JobStaffAssignment jx
                                        WHERE jx.jobID = j.jobID AND jx.staffID = @crew))";

        // Site evaluations (visible to Operations Manager and above only).
        private const string EvaluationSql = @"
            SELECT jr.jobRequestID, jr.siteEvaluationDate, jr.status, jr.siteAddress, jr.latitude, jr.longitude, jr.urgencyLevel,
                   LTRIM(ISNULL(c.clientName, '') + ' ' + ISNULL(c.clientSurname, '')) AS clientName,
                   c.phoneNumber AS clientPhone,
                   STUFF((SELECT ', ' + jt.jobTypeName
                          FROM dbo.RequestItem ri
                          INNER JOIN dbo.JobType jt ON jt.jobTypeID = ri.jobTypeID
                          WHERE ri.jobRequestID = jr.jobRequestID
                          ORDER BY jt.jobTypeName
                          FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, '') AS items
            FROM dbo.JobRequest jr
            LEFT JOIN dbo.Client c ON c.clientID = jr.clientID
            WHERE jr.siteEvaluationDate IS NOT NULL
              AND jr.siteEvaluationDate >= @from AND jr.siteEvaluationDate < @to
              AND ISNULL(jr.status, '') <> 'Cancelled'";

        // ------------------------------------------------------------ entry point
        public void ProcessRequest(HttpContext context)
        {
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);

            var cs = ConfigurationManager.ConnectionStrings["DefaultConnection"];
            if (cs == null || string.IsNullOrEmpty(cs.ConnectionString))
            {
                Fail(context, 500, "The calendar database is not configured.");
                return;
            }
            string connString = cs.ConnectionString;

            try
            {
                Me me = LoadMe(context, connString);
                if (me == null)
                {
                    Fail(context, 401, "Please sign in to view your calendar.");
                    return;
                }

                string action = (context.Request.QueryString["action"] ?? "events").Trim().ToLowerInvariant();
                switch (action)
                {
                    case "events":
                        {
                            AutoUpdateJobStatuses(connString);
                            DateTime from, to;
                            ParseRange(context.Request, out from, out to);
                            WriteJson(context, EventFeed(connString, me, ResolveScope(context.Request, me), from, to));
                            break;
                        }
                    case "stats":
                        AutoUpdateJobStatuses(connString);
                        WriteJson(context, Stats(connString, me, ResolveScope(context.Request, me)));
                        break;
                    case "staff":
                        WriteJson(context, StaffList(connString, me));
                        break;
                    case "ics":
                        WriteIcs(context, connString, me);
                        break;
                    default:
                        Fail(context, 400, "Unknown calendar action.");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("CalendarEvents failed: " + ex);
                Fail(context, 500, "The calendar could not be loaded. Please try again.");
            }
        }

        // ------------------------------------------------------------ who is asking
        private static Me LoadMe(HttpContext context, string connString)
        {
            if (context.Session == null) return null;
            object sid = context.Session["StaffID"];
            int id;
            if (sid == null || !int.TryParse(sid.ToString(), out id) || id <= 0) return null;

            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
                SELECT s.firstName, s.lastName, r.accessLevel
                FROM dbo.Staff s
                INNER JOIN dbo.Role r ON r.roleID = s.roleID
                WHERE s.staffID = @id AND RTRIM(LTRIM(ISNULL(s.staffStatus, ''))) = 'Active'", conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    string first = Str(r["firstName"]).Trim();
                    string last = Str(r["lastName"]).Trim();
                    return new Me
                    {
                        StaffId = id,
                        FirstName = first,
                        FullName = (first + " " + last).Trim(),
                        Level = Convert.ToInt32(r["accessLevel"])
                    };
                }
            }
        }

        private static Scope ResolveScope(HttpRequest req, Me me)
        {
            var scope = new Scope();
            if (me.CanSeeTeam && string.Equals(req.QueryString["scope"], "team", StringComparison.OrdinalIgnoreCase))
            {
                int filter;
                scope.Team = true;
                scope.FilterStaffId = int.TryParse(req.QueryString["staffId"], out filter) ? filter : 0;
            }
            return scope;
        }

        private static void ParseRange(HttpRequest req, out DateTime from, out DateTime to)
        {
            DateTime today = DateTime.Today;
            from = today.AddDays(-PastDays);
            to = today.AddDays(FutureDays);

            DateTime parsed;
            if (DateTime.TryParse(req.QueryString["start"], CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                from = parsed.Date;
            if (DateTime.TryParse(req.QueryString["end"], CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                to = parsed.Date;

            if (to <= from) to = from.AddDays(1);
            if ((to - from).TotalDays > MaxRangeDays) to = from.AddDays(MaxRangeDays);
        }

        // ------------------------------------------------------------ data
        private static List<CalEvent> FetchEvents(string connString, Me me, Scope scope, DateTime from, DateTime to)
        {
            int crewId = scope.Team ? scope.FilterStaffId : me.StaffId;
            var list = new List<CalEvent>();

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();

                using (var cmd = new SqlCommand(JobSql, conn))
                {
                    cmd.Parameters.Add("@me", SqlDbType.Int).Value = me.StaffId;
                    cmd.Parameters.Add("@crew", SqlDbType.Int).Value = crewId;
                    cmd.Parameters.Add("@from", SqlDbType.Date).Value = from;
                    cmd.Parameters.Add("@to", SqlDbType.Date).Value = to;
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new CalEvent
                            {
                                Kind = "job",
                                Id = Convert.ToInt32(r["jobID"]),
                                Date = Convert.ToDateTime(r["startDate"]).Date,
                                Status = Str(r["jobStatus"]),
                                Client = Str(r["clientName"]).Trim(),
                                Phone = Str(r["clientPhone"]),
                                Address = Str(r["siteAddress"]),
                                Items = Str(r["items"]),
                                Crew = Str(r["crew"]),
                                Urgency = Str(r["urgencyLevel"]),
                                RequestId = Int(r["jobRequestID"]),
                                Lat = Dbl(r["latitude"]),
                                Lng = Dbl(r["longitude"]),
                                TotalHours = Dbl(r["totalHours"]) ?? 0,
                                MyHours = Dbl(r["myHours"]) ?? 0,
                                IsMine = Int(r["isMine"]) == 1
                            });
                        }
                    }
                }

                // Site evaluations: managers only, and not when filtering to a single crew member.
                bool includeEvaluations = me.CanSeeTeam && (!scope.Team || scope.FilterStaffId == 0);
                if (includeEvaluations)
                {
                    using (var cmd = new SqlCommand(EvaluationSql, conn))
                    {
                        cmd.Parameters.Add("@from", SqlDbType.Date).Value = from;
                        cmd.Parameters.Add("@to", SqlDbType.Date).Value = to;
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                int requestId = Convert.ToInt32(r["jobRequestID"]);
                                list.Add(new CalEvent
                                {
                                    Kind = "evaluation",
                                    Id = requestId,
                                    RequestId = requestId,
                                    Date = Convert.ToDateTime(r["siteEvaluationDate"]).Date,
                                    Status = Str(r["status"]),
                                    Client = Str(r["clientName"]).Trim(),
                                    Phone = Str(r["clientPhone"]),
                                    Address = Str(r["siteAddress"]),
                                    Items = Str(r["items"]),
                                    Urgency = Str(r["urgencyLevel"]),
                                    Lat = Dbl(r["latitude"]),
                                    Lng = Dbl(r["longitude"])
                                });
                            }
                        }
                    }
                }
            }
            return list;
        }

        // ------------------------------------------------------------ responses
        private static object EventFeed(string connString, Me me, Scope scope, DateTime from, DateTime to)
        {
            return FetchEvents(connString, me, scope, from, to)
                .OrderBy(e => e.Date).ThenBy(e => e.Kind)
                .Select(ToDto)
                .ToList();
        }

        private static object Stats(string connString, Me me, Scope scope)
        {
            DateTime today = DateTime.Today;
            int offset = ((int)today.DayOfWeek + 6) % 7;     // Monday = start of week
            DateTime weekStart = today.AddDays(-offset);
            DateTime weekEnd = weekStart.AddDays(7);

            var events = FetchEvents(connString, me, scope, today.AddDays(-PastDays), today.AddDays(FutureDays));
            var jobs = events.Where(e => e.Kind == "job").ToList();
            var thisWeek = events.Where(e => e.Date >= weekStart && e.Date < weekEnd).ToList();
            var weekJobs = thisWeek.Where(e => e.Kind == "job").ToList();

            CalEvent next = events
                .Where(e => e.Date >= today && !(e.Kind == "job" && e.Status == "Completed"))
                .OrderBy(e => e.Date).ThenBy(e => e.Kind)
                .FirstOrDefault();

            return new Dictionary<string, object>
            {
                { "scope", scope.Team ? "team" : "me" },
                { "weekJobs", weekJobs.Count },
                { "weekHours", Math.Round(weekJobs.Sum(e => scope.Team ? e.TotalHours : e.MyHours), 1) },
                { "inProgress", jobs.Count(e => e.Status == "In Progress") },
                { "completedRecent", jobs.Count(e => e.Status == "Completed" && e.Date >= today.AddDays(-PastDays) && e.Date <= today) },
                { "evaluationsWeek", thisWeek.Count(e => e.Kind == "evaluation") },
                { "todayCount", events.Count(e => e.Date == today) },
                { "nextUp", next == null ? null : ToDto(next) }
            };
        }

        private static object StaffList(string connString, Me me)
        {
            var staff = new List<object>();
            if (me.CanSeeTeam)
            {
                using (var conn = new SqlConnection(connString))
                using (var cmd = new SqlCommand(@"
                    SELECT s.staffID, s.firstName, s.lastName, r.roleName
                    FROM dbo.Staff s
                    INNER JOIN dbo.Role r ON r.roleID = s.roleID
                    WHERE RTRIM(LTRIM(ISNULL(s.staffStatus, ''))) = 'Active'
                    ORDER BY s.firstName, s.lastName", conn))
                {
                    conn.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            staff.Add(new Dictionary<string, object>
                            {
                                { "id", Convert.ToInt32(r["staffID"]) },
                                { "name", (Str(r["firstName"]) + " " + Str(r["lastName"])).Trim() },
                                { "role", Str(r["roleName"]).Trim() }
                            });
                        }
                    }
                }
            }

            return new Dictionary<string, object>
            {
                { "me", new Dictionary<string, object> { { "id", me.StaffId }, { "name", me.FullName } } },
                { "canSeeTeam", me.CanSeeTeam },
                { "staff", staff }
            };
        }

        private static void WriteIcs(HttpContext context, string connString, Me me)
        {
            DateTime today = DateTime.Today;
            var personal = new Scope { Team = false, FilterStaffId = 0 };
            var events = FetchEvents(connString, me, personal, today.AddDays(-PastDays), today.AddDays(FutureDays));

            string origin = context.Request.Url.GetLeftPart(UriPartial.Authority);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

            var sb = new StringBuilder();
            sb.Append("BEGIN:VCALENDAR\r\n");
            sb.Append("VERSION:2.0\r\n");
            sb.Append("PRODID:-//Giant Group//Operations Calendar//EN\r\n");
            sb.Append("CALSCALE:GREGORIAN\r\n");
            sb.Append("METHOD:PUBLISH\r\n");
            sb.Append("X-WR-CALNAME:" + IcsText((me.FirstName.Length > 0 ? me.FirstName : "My") + " - Giant Group schedule") + "\r\n");

            foreach (var e in events.OrderBy(x => x.Date))
            {
                string path = e.Kind == "job"
                    ? VirtualPathUtility.ToAbsolute("~/ViewStaffAllocation.aspx")
                    : VirtualPathUtility.ToAbsolute("~/Requests/EvaluateRequest.aspx?id=" + e.RequestId);
                string summary = e.Kind == "job"
                    ? "Job #" + e.Id + " - " + (e.Client.Length > 0 ? e.Client : "client")
                    : "Site evaluation - " + (e.Client.Length > 0 ? e.Client : "client");

                var desc = new StringBuilder();
                desc.Append("Status: " + e.Status);
                if (e.Items.Length > 0) desc.Append("\n" + "Items: " + e.Items);
                if (e.Crew.Length > 0) desc.Append("\n" + "Crew: " + e.Crew);
                desc.Append("\n" + origin + path);

                sb.Append("BEGIN:VEVENT\r\n");
                sb.Append("UID:" + e.Kind + "-" + e.Id + "@giantgroup\r\n");
                sb.Append("DTSTAMP:" + stamp + "\r\n");
                sb.Append("DTSTART;VALUE=DATE:" + e.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "\r\n");
                sb.Append("DTEND;VALUE=DATE:" + e.Date.AddDays(1).ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "\r\n");
                sb.Append("SUMMARY:" + IcsText(summary) + "\r\n");
                if (e.Address.Length > 0) sb.Append("LOCATION:" + IcsText(e.Address) + "\r\n");
                sb.Append("DESCRIPTION:" + IcsText(desc.ToString()) + "\r\n");
                sb.Append("URL:" + origin + path + "\r\n");
                sb.Append("END:VEVENT\r\n");
            }
            sb.Append("END:VCALENDAR\r\n");

            context.Response.ContentType = "text/calendar";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.AddHeader("Content-Disposition", "attachment; filename=\"giant-group-my-schedule.ics\"");
            context.Response.Write(sb.ToString());
        }

        /// <summary>One event in the shape FullCalendar and the side panels expect.</summary>
        private static Dictionary<string, object> ToDto(CalEvent e)
        {
            bool job = e.Kind == "job";
            string client = e.Client.Length > 0 ? e.Client : "Unnamed client";
            string title = job ? "Job #" + e.Id + " · " + client : "Site evaluation · " + client;
            string requestUrl = e.RequestId > 0
                ? VirtualPathUtility.ToAbsolute("~/Requests/EvaluateRequest.aspx?id=" + e.RequestId)
                : null;
            string url = job ? VirtualPathUtility.ToAbsolute("~/ViewStaffAllocation.aspx") : requestUrl;

            return new Dictionary<string, object>
            {
                { "id", e.Kind + "-" + e.Id },
                { "title", title },
                { "start", e.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
                { "allDay", true },
                { "url", url },
                { "extendedProps", new Dictionary<string, object>
                    {
                        { "kind", e.Kind },
                        { "jobId", job ? e.Id : 0 },
                        { "requestId", e.RequestId },
                        { "status", e.Status },
                        { "client", client },
                        { "phone", e.Phone },
                        { "address", e.Address },
                        { "items", e.Items },
                        { "crew", e.Crew },
                        { "urgency", e.Urgency },
                        { "lat", e.Lat },
                        { "lng", e.Lng },
                        { "totalHours", e.TotalHours },
                        { "myHours", e.MyHours },
                        { "isMine", e.IsMine },
                        { "requestUrl", requestUrl }
                    }
                }
            };
        }

        // ------------------------------------------------------------ housekeeping
        /// <summary>Keeps the existing automatic job-status rules from the original calendar feed.</summary>
        private static void AutoUpdateJobStatuses(string connString)
        {
            const string updateQuery = @"
                -- 1. Any job scheduled for TODAY becomes 'In Progress'
                UPDATE Job
                SET jobStatus = 'In Progress'
                WHERE startDate IS NOT NULL
                  AND CAST(startDate AS DATE) = CAST(GETDATE() AS DATE)
                  AND (jobStatus = 'Not Started' OR jobStatus = 'Pending' OR jobStatus IS NULL);

                -- 2. Any job scheduled BEFORE TODAY automatically becomes 'Completed'
                UPDATE Job
                SET jobStatus = 'Completed'
                WHERE startDate IS NOT NULL
                  AND CAST(startDate AS DATE) < CAST(GETDATE() AS DATE)
                  AND jobStatus IN ('Not Started', 'Pending', 'In Progress', '');";

            try
            {
                using (var conn = new SqlConnection(connString))
                using (var cmd = new SqlCommand(updateQuery, conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                System.Diagnostics.Trace.TraceWarning("Auto-updating job statuses failed: " + ex.Message);
            }
        }

        // ------------------------------------------------------------ small helpers
        private static string Str(object o)
        {
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o);
        }

        private static int Int(object o)
        {
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        private static double? Dbl(object o)
        {
            return o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o);
        }

        private static string IcsText(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,")
                             .Replace("\r\n", "\\n").Replace("\n", "\\n");
        }

        private static void WriteJson(HttpContext context, object payload)
        {
            context.Response.ContentType = "application/json";
            context.Response.Write(new JavaScriptSerializer().Serialize(payload));
        }

        private static void Fail(HttpContext context, int code, string message)
        {
            context.Response.StatusCode = code;
            context.Response.ContentType = "application/json";
            context.Response.Write(new JavaScriptSerializer().Serialize(
                new Dictionary<string, object> { { "error", message } }));
        }
    }
}
