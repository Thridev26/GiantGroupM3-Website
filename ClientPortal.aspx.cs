using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class ClientPortal : Page
    {
        // Connection string name from Web.config
        private readonly string _connString = ConfigurationManager.ConnectionStrings["defaultConnection"]?.ConnectionString
            ?? @"Server=GroupWst1;Database=GroupWst1;Trusted_Connection=True;";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Never let the browser cache a signed-in dashboard (stops the Back button
            // showing a client's data after they have logged out).
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            // Security Guard: the user must hold a valid auth cookie AND a session identity
            if (!Request.IsAuthenticated || (Session["ClientID"] == null && Session["Username"] == null))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadClientData();
            }
        }

        private void LoadClientData()
        {
            // litClientName is declared with Mode="Encode", so the name is HTML-encoded on output
            litClientName.Text = Session["Username"]?.ToString() ?? "Client";
            litToday.Text = PortalNow().ToString("dddd, d MMMM yyyy", CultureInfo.GetCultureInfo("en-ZA"));

            // Get logged-in Client ID from session
            if (int.TryParse(Session["ClientID"]?.ToString(), out int clientID))
            {
                FetchDashboardMetrics(clientID);
            }
            else
            {
                // Fallback / default values if ClientID is missing
                litActiveRequests.Text = "0";
                litQuotesReady.Text = "0";
                litCompletedJobs.Text = "0";
                pnlEmpty.Visible = true;
            }
        }

        private void FetchDashboardMetrics(int clientID)
        {
            // One round trip for all three counts instead of three separate queries
            const string metricsSql = @"
                SELECT
                    (SELECT COUNT(*) FROM [dbo].[JobRequest]
                      WHERE clientID = @clientID
                        AND status IN ('Pending', 'Evaluation Date Set')) AS ActiveRequests,

                    (SELECT COUNT(*) FROM [dbo].[Quote] q
                      INNER JOIN [dbo].[JobRequest] jr ON q.jobRequestID = jr.jobRequestID
                      WHERE jr.clientID = @clientID
                        AND q.quoteStatus = 'Sent') AS QuotesReady,

                    (SELECT COUNT(*) FROM [dbo].[Quote] q
                      INNER JOIN [dbo].[JobRequest] jr ON q.jobRequestID = jr.jobRequestID
                      WHERE jr.clientID = @clientID
                        AND q.quoteStatus = 'Accepted') AS ApprovedQuotes;";

            const string recentSql = @"
                SELECT TOP 5 jobRequestID, status
                FROM [dbo].[JobRequest]
                WHERE clientID = @clientID
                ORDER BY jobRequestID DESC;";

            // Safe defaults so the page still renders if the database is unavailable
            litActiveRequests.Text = "0";
            litQuotesReady.Text = "0";
            litCompletedJobs.Text = "0";

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(metricsSql, conn))
                    {
                        cmd.Parameters.Add("@clientID", SqlDbType.Int).Value = clientID;
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                litActiveRequests.Text = Convert.ToString(r["ActiveRequests"], CultureInfo.InvariantCulture);
                                litQuotesReady.Text = Convert.ToString(r["QuotesReady"], CultureInfo.InvariantCulture);
                                litCompletedJobs.Text = Convert.ToString(r["ApprovedQuotes"], CultureInfo.InvariantCulture);
                            }
                        }
                    }

                    var recent = new DataTable();
                    using (SqlCommand cmd = new SqlCommand(recentSql, conn))
                    {
                        cmd.Parameters.Add("@clientID", SqlDbType.Int).Value = clientID;
                        using (var adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(recent);
                        }
                    }

                    if (recent.Rows.Count > 0)
                    {
                        rptRecent.DataSource = recent;
                        rptRecent.DataBind();
                    }
                    else
                    {
                        pnlEmpty.Visible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the detail server-side; show the client a friendly message only
                System.Diagnostics.Trace.TraceError("ClientPortal metrics failed: " + ex);
                pnlError.Visible = true;
            }
        }

        // Maps a request status to a colour chip (used by the markup)
        protected string StatusClass(object status)
        {
            switch ((status?.ToString() ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "pending":
                case "submitted":
                case "needs quotation":
                    return "is-pending";
                case "evaluation date set":
                case "scheduled":
                case "in progress":
                case "active":
                    return "is-active";
                case "accepted":
                case "approved":
                case "completed":
                    return "is-done";
                default:
                    return string.Empty;
            }
        }

        // Azure hosts run on UTC; show South African time to the user
        private static DateTime PortalNow()
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time"));
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }
    }
}
