using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class ManagementPortal : Page
    {
        private readonly string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;

        // Module visibility, mirrors the navigation rules in Site.master.cs so the
        // dashboard never offers a tile the user can't actually open.
        protected bool ShowClients { get; private set; }
        protected bool ShowQuotes { get; private set; }
        protected bool ShowAllocation { get; private set; }
        protected bool ShowJobs { get; private set; }
        protected bool ShowInvoicing { get; private set; }
        protected bool ShowReports { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Never let the browser cache a signed-in dashboard
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            // Security Guard: Ensure user is logged in as Staff / Admin
            string role = Session["UserRole"]?.ToString() ?? string.Empty;
            int level = 0;
            if (Session["UserLevel"] != null)
            {
                int.TryParse(Session["UserLevel"].ToString(), out level);
            }

            bool isStaff = role.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                           role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                           role.Equals("Owner", StringComparison.OrdinalIgnoreCase) ||
                           level >= 4;

            // Must also hold a valid auth cookie, not just a lingering session value
            if (!Request.IsAuthenticated || !isStaff)
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            // Level 0 with a staff role is treated as level 1, matching Site.master.cs
            ApplyModuleVisibility(level > 0 ? level : 1);

            if (!IsPostBack)
            {
                LoadDashboardData();
            }
        }

        private void ApplyModuleVisibility(int level)
        {
            ShowClients = level >= 6;                 // Levels 1, 4, 5 cannot see Clients
            ShowQuotes = level == 5 || level == 6;    // Levels 1 and 4 cannot see Quotations
            ShowInvoicing = level >= 6;               // Levels 1, 4, 5 cannot see Invoicing
            ShowReports = level >= 6;                 // Reports are Level 6 only
            ShowAllocation = true;                    // Allocation is available to all staff
            ShowJobs = true;                          // Job progress is available to all staff
        }

        private void LoadDashboardData()
        {
            // litStaffName is declared with Mode="Encode", so the name is HTML-encoded on output
            litStaffName.Text = Session["StaffName"]?.ToString() ?? Session["Username"]?.ToString() ?? "Manager";
            litUserLevel.Text = Session["UserLevel"]?.ToString() ?? "4";
            litToday.Text = PortalNow().ToString("dddd, d MMMM yyyy", CultureInfo.GetCultureInfo("en-ZA"));

            // Safe defaults so the page still renders if the database is unavailable
            litPendingRequests.Text = "0";
            litScheduledJobs.Text = "0";
            litPendingQuotes.Text = "0";
            litActiveClients.Text = "0";

            if (string.IsNullOrWhiteSpace(connString))
            {
                pnlError.Visible = true;
                return;
            }

            // One round trip for all four counts instead of four separate queries
            const string metricsSql = @"
                SELECT
                    (SELECT COUNT(*) FROM JobRequest WHERE status IN ('Pending', 'Submitted', 'Needs Quotation')) AS PendingRequests,
                    (SELECT COUNT(*) FROM JobRequest WHERE status IN ('Scheduled', 'In Progress', 'Active'))      AS ScheduledJobs,
                    (SELECT COUNT(*) FROM Quote      WHERE quoteStatus = 'Sent')                                  AS PendingQuotes,
                    (SELECT COUNT(*) FROM Client)                                                                 AS ActiveClients;";

            const string attentionSql = @"
                SELECT TOP 5 jobRequestID, status
                FROM JobRequest
                WHERE status IN ('Pending', 'Submitted', 'Needs Quotation')
                ORDER BY jobRequestID DESC;";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(metricsSql, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            litPendingRequests.Text = Convert.ToString(r["PendingRequests"], CultureInfo.InvariantCulture);
                            litScheduledJobs.Text = Convert.ToString(r["ScheduledJobs"], CultureInfo.InvariantCulture);
                            litPendingQuotes.Text = Convert.ToString(r["PendingQuotes"], CultureInfo.InvariantCulture);
                            litActiveClients.Text = Convert.ToString(r["ActiveClients"], CultureInfo.InvariantCulture);
                        }
                    }

                    var attention = new DataTable();
                    using (SqlCommand cmd = new SqlCommand(attentionSql, conn))
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(attention);
                    }

                    if (attention.Rows.Count > 0)
                    {
                        rptAttention.DataSource = attention;
                        rptAttention.DataBind();
                    }
                    else
                    {
                        pnlAttentionEmpty.Visible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the detail server-side; show staff a friendly message only
                System.Diagnostics.Trace.TraceError("ManagementPortal dashboard failed: " + ex);
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

        // Azure hosts run on UTC; show South African time to staff
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