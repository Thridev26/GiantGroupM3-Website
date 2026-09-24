using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class ManagementPortal : Page
    {
        private readonly string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
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

            if (!isStaff)
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadDashboardData();
            }
        }

        private void LoadDashboardData()
        {
            string staffName = Session["StaffName"]?.ToString() ?? Session["Username"]?.ToString() ?? "Manager";
            litStaffName.Text = staffName;
            litUserLevel.Text = Session["UserLevel"]?.ToString() ?? "4";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // 1. Pending Requests (Needs quotation / Submitted status)
                string sqlPendingRequests = "SELECT COUNT(*) FROM JobRequest WHERE status IN ('Pending', 'Submitted', 'Needs Quotation')";
                using (SqlCommand cmd = new SqlCommand(sqlPendingRequests, conn))
                {
                    litPendingRequests.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                }

                // 2. Scheduled Jobs (Active jobs currently scheduled/assigned)
                string sqlScheduledJobs = "SELECT COUNT(*) FROM JobRequest WHERE status IN ('Scheduled', 'In Progress', 'Active')";
                using (SqlCommand cmd = new SqlCommand(sqlScheduledJobs, conn))
                {
                    litScheduledJobs.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                }

                // 3. Quotes Awaiting Approval (Sent to clients)
                string sqlPendingQuotes = "SELECT COUNT(*) FROM Quote WHERE quoteStatus = 'Sent'";
                using (SqlCommand cmd = new SqlCommand(sqlPendingQuotes, conn))
                {
                    litPendingQuotes.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                }

                // 4. Total Active Clients
                string sqlActiveClients = "SELECT COUNT(*) FROM Client";
                using (SqlCommand cmd = new SqlCommand(sqlActiveClients, conn))
                {
                    litActiveClients.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                }
            }
        }
    }
}