using System;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class ClientPortal : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Security Guard: Ensure user is logged in
            if (Session["Username"] == null && Session["UserRole"] == null)
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
            string clientName = Session["Username"]?.ToString() ?? "Client";
            litClientName.Text = clientName;

            // Placeholders for dynamic database counts
            litActiveRequests.Text = "1";
            litQuotesReady.Text = "1";
            litCompletedJobs.Text = "4";
        }
    }
}