using System;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class ManagementPortal : Page
    {
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
                // Redirect unauthorized clients or guests to Login
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

            //// Placeholders for dynamic database counts
            //litPendingRequests.Text = "5";
            //litScheduledJobs.Text = "3";
            //litPendingQuotes.Text = "2";
            //litActiveClients.Text = "14";
        }
    }
}